// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// An interface for an audio sample.
    /// </summary>
    public interface ISample : IAdjustableAudioComponent
    {
        /// <summary>
        /// The length in milliseconds of this <see cref="ISample"/>.
        /// </summary>
        double Length { get; }

        /// <summary>
        /// The number of <see cref="SampleChannel"/>s allowed to be played back simultaneously from this <see cref="ISample"/>.
        /// </summary>
        int PlaybackConcurrency { get; set; }

        /// <summary>
        /// Creates a new unique playback channel for this <see cref="ISample"/> and immediately plays it.
        /// </summary>
        /// <remarks>
        /// Multiple channels can be played simultaneously, but can only be heard up to <see cref="PlaybackConcurrency"/> times.
        /// </remarks>
        /// <returns>The unique <see cref="SampleChannel"/> for the playback.</returns>
        SampleChannel Play();

        /// <summary>
        /// Retrieves a unique playback channel for this <see cref="ISample"/>.
        /// </summary>
        /// <remarks>
        /// Multiple channels can be retrieved and played simultaneously, but can only be heard up to <see cref="PlaybackConcurrency"/> times.
        /// </remarks>
        /// <returns>The unique <see cref="SampleChannel"/> for the playback.</returns>
        SampleChannel GetChannel();
    }
}
