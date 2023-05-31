// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework
{
    public class LatencyFlex
    {
        public ulong TargetFrameTime = 0;

        private const int max_inflight_frames = 16;
        private const double up_factor = 1.1;
        private const double down_factor = 0.985;

        private readonly EwmaEstimator latency = new EwmaEstimator(0.3);
        private readonly EwmaEstimator invThroughput = new EwmaEstimator(0.3);
        private readonly EwmaEstimator projCorrection = new EwmaEstimator(0.5, true);

        private readonly ulong[] frameBeginTs = new ulong[max_inflight_frames];
        private readonly ulong[] frameBeginIds = new ulong[max_inflight_frames];
        private readonly ulong[] frameEndProjectedTs = new ulong[max_inflight_frames];
        private ulong frameEndProjectionBase = ulong.MaxValue;
        private readonly long[] compApplied = new long[max_inflight_frames];
        private ulong prevFrameBeginId = ulong.MaxValue;
        private long prevPredictionError;
        private ulong prevFrameEndId = ulong.MaxValue;
        private ulong prevFrameEndTs;
        private ulong prevFrameRealEndTs;

        public LatencyFlex()
        {
            frameBeginIds.AsSpan().Fill(ulong.MaxValue);
        }

        /// <summary>
        /// Get the desired wake-up time. Sleep until this time, then call `BeginFrame()`. This function
        /// must be called *exactly once* before each call to `BeginFrame()`. Calling this the second time
        /// with the same `frame_id` will corrupt the internal time tracking.
        ///
        /// If a wait target cannot be determined due to lack of data, then `0` is
        /// returned.
        /// </summary>
        public ulong GetWaitTarget(ulong frameId)
        {
            if (prevFrameEndId != ulong.MaxValue)
            {
                Phases phase = (Phases)(frameId % (ulong)Phases.NumPhases);
                double invtpt = invThroughput.Get();
                int compToApply = 0;

                if (frameEndProjectionBase == ulong.MaxValue)
                {
                    frameEndProjectionBase = prevFrameEndTs;
                }
                else
                {
                    // The prediction error is equal to (actual latency) - (expected latency).
                    // As we adapt our latency estimator to the actual latency values, this
                    // will eventually converge as long as we are not constantly overpacing,
                    // building a queue at a faster pace than the estimator can adapt.

                    // In the section below, we attempt to apply additional compensation in
                    // the case of delay increase, to prevent extra queuing as much as possible.
                    long predictionError =
                        (long)prevFrameEndTs -
                        (long)(frameEndProjectionBase +
                               frameEndProjectedTs[prevFrameEndId % max_inflight_frames]);
                    // TRACE_COUNTER("latencyflex", "Prediction error", predictionError);
                    long prevCompApplied = compApplied[prevFrameEndId % max_inflight_frames];
                    // We need to limit the compensation to delay increase, or otherwise we would cancel out the
                    // regular delay decrease from our pacing. To achieve this, we treat any early prediction as
                    // having prediction error of zero.
                    //
                    // We also want to cancel out the counter-reaction from our previous compensation, so what
                    // we essentially want here is `prediction_error_ - prev_prediction_error_ +
                    // prev_comp_applied`. But since we clamp prediction_error_ and prev_prediction_error_,
                    // the naive approach of adding prev_comp_applied directly would have a bias toward
                    // overcompensation. Consider the example below where we're pacing at the correct (100%)
                    // rate but things arrives late due to reason that are *not* queuing (noise):
                    // 5ms late, 5ms late, ... (a period longer than our latency) ... , 0ms
                    // We would compensate -5ms on the first frame, bringing the prediction error to 0. But when
                    // the 0ms frame arrives, the prediction error becomes -5ms due to our overcompensation.
                    // Due to its negativity, we don't recompensate for this decrease: this is the bias.
                    //
                    // The solution here is to include prev_comp_applied as a part of clamping equation, which
                    // allows it to also undercompensate when it makes sense. It seems to do a great job on
                    // preventing prediction error from getting stuck in a state that is drift away.
                    projCorrection.Update(
                        Math.Max(0, predictionError) -
                        Math.Max(0, prevPredictionError - prevCompApplied));
                    prevPredictionError = predictionError;
                    // Try to cancel out any unintended delay happened to previous frame start. This is
                    // primarily meant for cases where a frame time spike happens and we get backpressured
                    // on the main thread. prev_forced_correction_ will stay high until our prediction catches
                    // up, canceling out any excessive correction we might end up doing.
                    compToApply = (int)Math.Round(projCorrection.Get());
                    compApplied[frameId % max_inflight_frames] = compToApply;
                    // TRACE_COUNTER("latencyflex", "Delay Compensation", compToApply);
                }

                // The target wakeup time.
                ulong target =
                    (ulong)(
                        (long)frameEndProjectionBase +
                        (long)frameEndProjectedTs[prevFrameBeginId % max_inflight_frames] +
                        compToApply +
                        (long)Math.Round((((long)frameId - (long)prevFrameBeginId) +
                                             1 / (phase == Phases.Up ? up_factor : 1) - 1) *
                                         invtpt / down_factor -
                                         latency.Get()));
                // The projection is something close to the predicted frame end time, but it is always paced
                // at down_factor * throughput, which prevents delay compensation from kicking in until it's
                // actually necessary (i.e. we're overpacing).
                ulong newProjection =
                    (ulong)(
                        (long)frameEndProjectedTs[prevFrameBeginId % max_inflight_frames] +
                        compToApply +
                        (long)Math.Round(((long)frameId - (long)prevFrameBeginId) * invtpt /
                                         down_factor));
                frameEndProjectedTs[frameId % max_inflight_frames] = newProjection;
                // TRACE_EVENT_BEGIN(
                //     "latencyflex", "projection",
                //     perfetto::Track(track_base_ + frame_id % kMaxInflightFrames + kMaxInflightFrames),
                //     target);
                // TRACE_EVENT_END(
                //     "latencyflex",
                //     perfetto::Track(track_base_ + frame_id % kMaxInflightFrames + kMaxInflightFrames),
                //     frame_end_projection_base_ + newProjection);
                return target;
            }

            return 0;
        }

        /// <summary>
        /// Begin the frame. Called on the main/simulation thread.
        ///
        /// This call must be preceded with a call to `GetWaitTarget()`.
        ///
        /// `target` should be the timestamp returned by `GetWaitTarget()`.
        /// `timestamp` should be calculated as follows:
        /// - If a sleep is not performed (because the wait target has already been
        ///   passed), then pass the current time.
        /// - If a sleep is performed (wait target was not in the past), then pass the
        ///   wait target as-is. This allows compensating for any latency incurred by
        ///   the OS for waking up the process.
        /// </summary>
        public void BeginFrame(ulong frameId, ulong target, ulong timestamp)
        {
            // TRACE_EVENT_BEGIN("latencyflex", "frame",
            //     perfetto::Track(track_base_ + frame_id % kMaxInflightFrames), timestamp);
            frameBeginIds[frameId % max_inflight_frames] = frameId;
            frameBeginTs[frameId % max_inflight_frames] = timestamp;
            prevFrameBeginId = frameId;

            if (target != 0)
            {
                long forcedCorrection = (long)(timestamp - target);
                frameEndProjectedTs[frameId % max_inflight_frames] += (ulong)forcedCorrection;
                compApplied[frameId % max_inflight_frames] += forcedCorrection;
                prevPredictionError += forcedCorrection;
            }
        }

        /// <summary>
        /// End the frame. Called from a rendering-related thread.
        ///
        /// The timestamp should be obtained in one of the following ways:
        /// - Run a thread dedicated to wait for command buffer completion fences.
        ///   Capture the timestamp on CPU when the fence is signaled.
        /// - Capture a GPU timestamp when frame ends, then convert it into a clock
        ///   domain on CPU (known as "timestamp calibration").
        ///
        /// If `latency` and `frame_time` are not null, then the latency and the frame
        /// time are returned respectively, or UINT64_MAX is returned if measurement is
        /// unavailable.
        /// </summary>
        public void EndFrame(ulong frameId, ulong timestamp, out ulong latency, out ulong frameTime)
        {
            latency = 0;

            Phases phase = (Phases)(frameId % (ulong)Phases.NumPhases);
            long frameTimeVal = -1;

            if (frameBeginIds[frameId % max_inflight_frames] == frameId)
            {
                frameBeginIds[frameId % max_inflight_frames] = ulong.MaxValue;

                if (prevFrameEndId != ulong.MaxValue)
                    frameTime = timestamp - prevFrameRealEndTs;
                prevFrameRealEndTs = timestamp;
                timestamp = Math.Max(timestamp, prevFrameEndTs + TargetFrameTime);
                ulong frameStart = frameBeginTs[frameId % max_inflight_frames];
                long latencyVal = (long)timestamp - (long)frameStart;

                if (phase == Phases.Down)
                {
                    this.latency.Update(latencyVal);
                }

                latency = (ulong)latencyVal;
                // TRACE_COUNTER("latencyflex", "Latency", latencyVal);
                // TRACE_COUNTER("latencyflex", "Latency (Estimate)", this.latency.Get());

                if (prevFrameEndId != ulong.MaxValue)
                {
                    if (frameId > prevFrameEndId)
                    {
                        ulong framesElapsed = frameId - prevFrameEndId;
                        frameTimeVal =
                            ((long)timestamp - (long)prevFrameEndTs) / (long)framesElapsed;
                        frameTimeVal = Math.Clamp(frameTimeVal, 1000000, 50000000);

                        if (phase == Phases.Up)
                        {
                            invThroughput.Update(frameTimeVal);
                        }

                        // TRACE_COUNTER("latencyflex", "Frame Time", frameTimeVal);
                        // TRACE_COUNTER("latencyflex", "Frame Time (Estimate)", invThroughput.Get());
                    }
                }

                prevFrameEndId = frameId;
                prevFrameEndTs = timestamp;
            }

            frameTime = (ulong)frameTimeVal;
            // TRACE_EVENT_END("latencyflex", perfetto::Track(track_base_ + frame_id % kMaxInflightFrames),
            //     timestamp);
        }
    }

    public class EwmaEstimator
    {
        private readonly double alpha;
        private double current;
        private double currentWeight;

        public EwmaEstimator(double alpha, bool fullWeight = false)
        {
            this.alpha = alpha;
            currentWeight = fullWeight ? 1 : 0;
        }

        public void Update(double value)
        {
            current = (1 - alpha) * current + alpha * value;
            currentWeight = (1 - alpha) * currentWeight + alpha;
        }

        public double Get()
        {
            if (currentWeight == 0)
                return 0;

            return currentWeight / currentWeight;
        }
    }

    public enum Phases
    {
        Up,
        Down,
        NumPhases
    }
}
