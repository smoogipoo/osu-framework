// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using ManagedBass;

namespace osu.Framework.Audio.Mixing.Bass
{
    internal class PassthroughBassAudioMixer : AudioMixer, IBassAudio, IBassAudioMixer
    {
        private readonly List<IBassAudioChannel> activeChannels = new List<IBassAudioChannel>();

        public PassthroughBassAudioMixer(AudioManager manager, AudioMixer? fallbackMixer, string identifier)
            : base(fallbackMixer, identifier)
        {
        }

        public override void AddEffect(IEffectParameter effect, int priority = 0)
        {
        }

        public override void RemoveEffect(IEffectParameter effect)
        {
        }

        public override void UpdateEffect(IEffectParameter effect)
        {
        }

        protected override void AddInternal(IAudioChannel channel)
        {
            if (!(channel is IBassAudioChannel bassChannel))
                return;

            if (bassChannel.Handle == 0)
                return;

            activeChannels.Add(bassChannel);

            if (ManagedBass.Bass.ChannelIsActive(bassChannel.Handle) != PlaybackState.Stopped)
                ManagedBass.Bass.ChannelPlay(bassChannel.Handle);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            if (!(channel is IBassAudioChannel bassChannel))
                return;

            if (bassChannel.Handle == 0)
                return;

            activeChannels.Remove(bassChannel);
            ManagedBass.Bass.ChannelPause(bassChannel.Handle);
        }

        public void UpdateDevice(int deviceIndex)
        {
        }

        public BassFlags SampleFlags => BassFlags.Default;
        public BassFlags TrackFlags => BassFlags.Default;

        public bool ChannelPlay(IBassAudioChannel channel, bool restart = false)
        {
            if (channel.Handle == 0)
                return false;

            return ManagedBass.Bass.ChannelPlay(channel.Handle, restart);
        }

        public bool ChannelPause(IBassAudioChannel channel, bool flushMixer = false)
            => ManagedBass.Bass.ChannelPause(channel.Handle);

        public PlaybackState ChannelIsActive(IBassAudioChannel channel)
            => ManagedBass.Bass.ChannelIsActive(channel.Handle);

        public long ChannelGetPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes)
            => ManagedBass.Bass.ChannelGetPosition(channel.Handle, mode);

        public bool ChannelSetPosition(IBassAudioChannel channel, long position, PositionFlags mode = PositionFlags.Bytes)
            => ManagedBass.Bass.ChannelSetPosition(channel.Handle, position, mode);

        public bool ChannelGetLevel(IBassAudioChannel channel, float[] levels, float length, LevelRetrievalFlags flags)
            => ManagedBass.Bass.ChannelGetLevel(channel.Handle, levels, length, flags);

        public int ChannelGetData(IBassAudioChannel channel, float[] buffer, int length)
            => ManagedBass.Bass.ChannelGetData(channel.Handle, buffer, length);

        public int ChannelSetSync(IBassAudioChannel channel, SyncFlags type, long parameter, SyncProcedure procedure, IntPtr user = default)
            => ManagedBass.Bass.ChannelSetSync(channel.Handle, type, parameter, procedure, user);

        public bool ChannelRemoveSync(IBassAudioChannel channel, int sync)
            => ManagedBass.Bass.ChannelRemoveSync(channel.Handle, sync);

        public bool StreamFree(IBassAudioChannel channel)
        {
            ManagedBass.Bass.ChannelStop(channel.Handle);
            return ManagedBass.Bass.StreamFree(channel.Handle);
        }

        public void AddChannelToBassMix(IBassAudioChannel channel)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var channel in activeChannels.ToArray())
                ManagedBass.Bass.ChannelPause(channel.Handle);
            activeChannels.Clear();
        }
    }
}
