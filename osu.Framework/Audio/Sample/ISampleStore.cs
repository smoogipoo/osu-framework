// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    public interface ISampleStore : IAdjustableResourceStore<Sample>
    {
        /// <summary>
        /// The default number of <see cref="SampleChannel"/>s allowed to be played back simultaneously from individual <see cref="Sample"/>s.
        /// </summary>
        int DefaultPlaybackConcurrency { get; set; }
    }
}
