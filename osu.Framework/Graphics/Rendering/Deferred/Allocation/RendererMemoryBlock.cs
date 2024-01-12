// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal readonly record struct RendererMemoryBlock(int BufferId, int Index, int Length)
    {
        public Span<byte> GetBuffer(DeferredRenderer renderer) => renderer.GetBuffer(this);
    }
}
