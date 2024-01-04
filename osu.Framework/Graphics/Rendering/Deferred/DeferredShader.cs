// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredShader : IShader
    {
        public IShader Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredShader(DeferredRenderer renderer, IShader shader)
        {
            this.renderer = renderer;
            Resource = shader;
        }

        public IReadOnlyDictionary<string, IUniform> Uniforms => Resource.Uniforms;

        public void Bind() => renderer.RenderEvents.Add(new BindShaderEvent(this));

        public void Unbind() => renderer.RenderEvents.Add(new UnbindShaderEvent(this));

        public bool IsLoaded => Resource.IsLoaded;

        public bool IsBound => false; // Todo: Could be done better but this is never used right now.

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
            => new DeferredUniform<T>(renderer, this, Resource.GetUniform<T>(name));

        public void BindUniformBlock(string blockName, IUniformBuffer buffer)
            => renderer.RenderEvents.Add(new BindUniformBlockEvent(this, blockName, buffer));

        public void Dispose()
        {
            // Todo:
        }
    }
}
