// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredUniform<T> : Uniform<T>
        where T : unmanaged, IEquatable<T>
    {
        public readonly Uniform<T> Uniform;

        public DeferredUniform(IRenderer renderer, IShader owner, Uniform<T> uniform)
            : base(renderer, owner, string.Empty, 0)
        {
            Uniform = uniform;
        }
    }
}
