// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Sample
{
    public abstract class Sample : AudioCollectionManager<SampleChannel>, ISample
    {
        public const int DEFAULT_CONCURRENCY = 2;

        public double Length { get; protected set; }

        internal Action<Sample> OnPlay;

        private int playbackConcurrency = DEFAULT_CONCURRENCY;

        public int PlaybackConcurrency
        {
            get => playbackConcurrency;
            set
            {
                playbackConcurrency = value;
                updateSampleConcurrency();
            }
        }

        public SampleChannel Play()
        {
            var channel = GetChannel();
            channel.Play();
            return channel;
        }

        public SampleChannel GetChannel()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not get a channel from a disposed sample.");

            var channel = CreateChannel();

            if (channel != null)
                channel.OnPlay = onPlay;

            return channel;
        }

        private void onPlay(SampleChannel channel)
        {
            AddItem(channel);
            updateSampleConcurrency();
            OnPlay?.Invoke(this);
        }

        private void updateSampleConcurrency() => EnqueueAction(() =>
        {
            int countPlaying = 0;

            // We're concerned only of channels that are playing and have not been requested to stop.
            foreach (var item in Items)
            {
                if (item.Playing && !item.Stopping)
                    countPlaying++;
            }

            if (countPlaying <= PlaybackConcurrency)
                return;

            foreach (var item in Items)
            {
                if (item.Playing && !item.Stopping)
                {
                    item.Stop();
                    countPlaying--;
                }

                if (countPlaying == PlaybackConcurrency)
                    break;
            }
        });

        public override bool IsAlive => base.IsAlive && (!PendingActions.IsEmpty || Items.Count > 0);

        /// <summary>
        /// Creates a unique playback of this <see cref="Sample"/>.
        /// </summary>
        /// <returns>The <see cref="SampleChannel"/> for the playback.</returns>
        protected abstract SampleChannel CreateChannel();
    }
}
