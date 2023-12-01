// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredUniformBuffer<TData> : IUniformBuffer<TData>
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly DeferredRenderer renderer;

        public DeferredUniformBuffer(DeferredRenderer renderer)
        {
            this.renderer = renderer;
        }

        private TData data;

        public TData Data
        {
            get => data;
            set
            {
                data = value;
                renderer.RenderEvents.Add(new SetUniformBufferDataEvent<TData>(this, value));
            }
        }

        public void Dispose()
        {
            // Todo:
        }
    }
}
