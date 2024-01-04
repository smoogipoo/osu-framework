// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct SetUniformBufferDataEvent<TData>(DeferredUniformBuffer<TData> Buffer, TData Data) : IEvent
        where TData : unmanaged, IEquatable<TData>
    {
        public void Run(DeferredRenderer current, IRenderer target)
        {
            Buffer.Resource.Data = Data;
        }
    }
}
