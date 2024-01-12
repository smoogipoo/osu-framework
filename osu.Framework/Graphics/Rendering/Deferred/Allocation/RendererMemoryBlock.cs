// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal readonly record struct RendererMemoryBlock(int BufferId, int Index, int Length)
    {
        public Span<byte> GetBuffer(DeferredRenderer renderer) => renderer.GetBuffer(this);
    }

    internal readonly record struct RendererStagingMemoryBlock(int BufferId, int Index, int Length)
    {
        public void CopyTo(DeferredRenderer renderer, CommandList commandList, DeviceBuffer target, int offsetInTarget)
        {
            renderer.CopyStagingBuffer(this, commandList, target, offsetInTarget);
        }
    }
}
