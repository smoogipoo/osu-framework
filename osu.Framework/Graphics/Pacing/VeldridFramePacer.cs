// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using SDL;
using Veldrid;

namespace osu.Framework.Graphics.Pacing
{
    public class VeldridFramePacer : FramePacer
    {
        private readonly Stopwatch frameStopwatch = new Stopwatch();

        public void BeginFrame()
        {
            frameStopwatch.Restart();
        }

        public void EndFrame()
        {
            frameStopwatch.Stop();
        }

        public override void Delay(TimeSpan maxDuration)
        {
            // To get as up-to-date input as possible, we'll delay drawing the current frame until as close to the Vsync interval as possible.
            // A simple heuristic is to assume that frame times are generally static or ramping between frames.
            // But we definitely do not want to miss the Vsync interval, so we apply a little lenience.
            TimeSpan timeToWake = maxDuration - frameStopwatch.Elapsed - TimeSpan.FromMilliseconds(1);

            if (timeToWake.Ticks <= 0)
                return;

            SDL3.SDL_DelayPrecise((ulong)timeToWake.TotalNanoseconds);
        }
    }
}
