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
        private readonly DeferredRenderer renderer;
        private readonly IShader shader;

        public DeferredShader(DeferredRenderer renderer, IShader shader)
        {
            this.renderer = renderer;
            this.shader = shader;
        }

        public IReadOnlyDictionary<string, IUniform> Uniforms => shader.Uniforms;

        public void Bind() => renderer.RenderEvents.Add(new BindShaderEvent(shader));

        public void Unbind() => renderer.RenderEvents.Add(new UnbindShaderEvent(shader));

        public bool IsLoaded => shader.IsLoaded;

        public bool IsBound => false; // Todo: Could be done better but this is never used right now.

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
            => new DeferredUniform<T>(renderer, this, shader.GetUniform<T>(name));

        public void BindUniformBlock(string blockName, IUniformBuffer buffer)
            => renderer.RenderEvents.Add(new BindUniformBlockEvent(blockName, buffer));

        public void Dispose()
        {
            // Todo:
        }
    }
}
