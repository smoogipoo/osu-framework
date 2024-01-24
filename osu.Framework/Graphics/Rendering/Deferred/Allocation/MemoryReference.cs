// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Development;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal readonly record struct MemoryReference(int BufferId, int Index, int Length)
    {
        public Span<byte> GetRegion(DeferredRenderer renderer)
            => renderer.GetRegion(this);

        public void WriteTo(DeferredRenderer renderer, MappedResource target, int offsetInTarget)
        {
            ThreadSafety.EnsureDrawThread();

            unsafe
            {
                Span<byte> targetSpan = new Span<byte>(target.Data.ToPointer(), (int)target.SizeInBytes);
                GetRegion(renderer).CopyTo(targetSpan[offsetInTarget..]);
            }
        }

        public void WriteTo(DeferredRenderer renderer, DeviceBuffer target, int offsetInTarget)
        {
            ThreadSafety.EnsureDrawThread();
            renderer.Device.UpdateBuffer(target, (uint)offsetInTarget, GetRegion(renderer));
        }
    }
}
