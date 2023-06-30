// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    internal class DummyArrayBuffer<T> : IArrayBuffer<T>
        where T : unmanaged, IEquatable<T>
    {
        public void Dispose()
        {
        }

        public T this[int index]
        {
            get => default;
            set { }
        }
    }
}
