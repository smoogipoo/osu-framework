// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredShader : IShader
    {
        public VeldridShader Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredShader(DeferredRenderer renderer, VeldridShader shader)
        {
            this.renderer = renderer;
            Resource = shader;
        }

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms { get; } = new Dictionary<string, IUniform>();

        public void Bind()
        {
            if (IsBound)
                return;

            renderer.BindShader(this);
            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public bool IsLoaded => Resource.IsLoaded;

        public bool IsBound { get; private set; }

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
            => new DeferredUniform<T>(renderer, this, Resource.GetUniform<T>(name));

        public void BindUniformBlock(string blockName, IUniformBuffer buffer)
            => renderer.EnqueueEvent(BindUniformBlockEvent.Create(renderer, this, blockName, (IDeferredUniformBuffer)buffer));

        public void Dispose()
        {
            // Todo:
        }
    }
}
