﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class QuadIndexData
    {
        static QuadIndexData()
        {
            GL.GenBuffers(1, out EBO_ID);
        }

        public static readonly int EBO_ID;
        public static int MaxAmountIndices;
    }

    public class QuadVertexBuffer<T> : VertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly OpenGLRenderer renderer;
        private readonly int amountIndices;

        private const int indices_per_quad = IRenderer.VERTICES_PER_QUAD + 2;

        /// <summary>
        /// The maximum number of quads supported by this buffer.
        /// </summary>
        public const int MAX_QUADS = ushort.MaxValue / indices_per_quad;

        internal QuadVertexBuffer(OpenGLRenderer renderer, int amountQuads, BufferUsageHint usage)
            : base(renderer, amountQuads * IRenderer.VERTICES_PER_QUAD, usage)
        {
            this.renderer = renderer;
            amountIndices = amountQuads * indices_per_quad;
            Debug.Assert(amountIndices <= ushort.MaxValue);
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountIndices > QuadIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountIndices];

                for (ushort i = 0, j = 0; j < amountIndices; i += IRenderer.VERTICES_PER_QUAD, j += indices_per_quad)
                {
                    indices[j] = i;
                    indices[j + 1] = (ushort)(i + 1);
                    indices[j + 2] = (ushort)(i + 3);
                    indices[j + 3] = (ushort)(i + 2);
                    indices[j + 4] = (ushort)(i + 3);
                    indices[j + 5] = (ushort)(i + 1);
                }

                renderer.BindBuffer(BufferTarget.ElementArrayBuffer, QuadIndexData.EBO_ID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(amountIndices * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                QuadIndexData.MaxAmountIndices = amountIndices;
            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                renderer.BindBuffer(BufferTarget.ElementArrayBuffer, QuadIndexData.EBO_ID);
        }

        protected override int ToElements(int vertices) => 3 * vertices / 2;

        protected override int ToElementIndex(int vertexIndex) => 3 * vertexIndex / 2;

        protected override PrimitiveType Type => PrimitiveType.Triangles;
    }
}
