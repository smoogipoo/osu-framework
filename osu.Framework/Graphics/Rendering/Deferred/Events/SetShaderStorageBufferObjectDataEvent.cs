// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetShaderStorageBufferObjectDataEvent(ResourceReference Buffer, int Index, MemoryReference Memory) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.SetShaderStorageBufferObjectData;

        public static SetShaderStorageBufferObjectDataEvent Create<T>(DeferredRenderer renderer, IDeferredShaderStorageBufferObject buffer, int index, T data)
            where T : unmanaged, IEquatable<T>
        {
            return new SetShaderStorageBufferObjectDataEvent(renderer.Reference(buffer), index, renderer.AllocateObject(data));
        }
    }
}
