// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches.Internal
{
    public class VertexBufferList<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public event Action<VertexBuffer<T>>? OnSpill;

        private readonly List<VertexBuffer<T>> buffers = new List<VertexBuffer<T>>();
        private readonly Func<VertexBuffer<T>> createBufferFunc;
        private readonly int maxBuffers;

        public int CurrentBufferIndex { get; private set; }
        public int CurrentVertexIndex => !hasSpace() ? 0 : getCurrentBuffer().Count;

        public VertexBufferList(int maxBuffers, Func<VertexBuffer<T>> createBufferFunc)
        {
            if (maxBuffers <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBuffers), maxBuffers, "Maximum number of vertex buffers must be greater than 0.");

            this.maxBuffers = maxBuffers;
            this.createBufferFunc = createBufferFunc;
        }

        public void Reset()
        {
            // If the current vertex buffer has any vertices remaining, spill it.
            if (hasSpace() && getCurrentBuffer().Count > 0)
                Spill();

            CurrentBufferIndex = 0;
        }

        public void Push()
        {
            ensureHasSpace();
            getCurrentBuffer().Push();
            checkForSpill();
        }

        public void Push(T vertex)
        {
            ensureHasSpace();
            getCurrentBuffer().Push(vertex);
            checkForSpill();
        }

        public void Spill()
        {
            if (!hasSpace())
                return;

            if (getCurrentBuffer().Count == 0)
                return;

            OnSpill?.Invoke(getCurrentBuffer());
            CurrentBufferIndex++;

            // Wrap back to 0 if we can't fit any more buffers.
            if (CurrentBufferIndex == maxBuffers)
                CurrentBufferIndex = 0;
        }

        private void ensureHasSpace()
        {
            if (!hasSpace())
                buffers.Add(createBufferFunc());
        }

        private void checkForSpill()
        {
            Debug.Assert(hasSpace());

            if (getCurrentBuffer().Count == getCurrentBuffer().Capacity)
                Spill();
        }

        private VertexBuffer<T> getCurrentBuffer() => buffers[CurrentBufferIndex];
        private bool hasSpace() => CurrentBufferIndex < buffers.Count;

        public void Dispose()
        {
            foreach (VertexBuffer<T> vbo in buffers)
                vbo.Dispose();
            buffers.Clear();

            CurrentBufferIndex = 0;
        }
    }
}
