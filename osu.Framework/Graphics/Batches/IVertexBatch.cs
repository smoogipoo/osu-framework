﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Batches
{
    public interface IVertexBatch
    {
        int Draw();

        void ResetCounters();

        internal void Advance();
    }
}
