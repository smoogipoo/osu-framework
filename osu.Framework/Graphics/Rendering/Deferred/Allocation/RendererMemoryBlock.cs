// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal readonly record struct RendererMemoryBlock(int BufferId, int Index, int Length)
    {
        public Span<byte> GetRegion(DeferredRenderer renderer) => renderer.GetRegion(this);
    }

    internal readonly record struct RendererStagingMemoryBlock(int BufferId, int Index, int Length, bool IsTemporary)
    {
        public void WriteTo(DeferredRenderer renderer, DeviceBuffer target, int offsetInTarget, CommandList commandList)
        {
            renderer.WriteRegionToBuffer(this, target, offsetInTarget, commandList);
        }
    }
}
