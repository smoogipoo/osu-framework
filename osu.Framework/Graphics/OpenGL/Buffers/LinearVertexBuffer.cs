﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Batches;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class LinearIndexData
    {
        static LinearIndexData()
        {
            GL.GenBuffers(1, out EBO_ID);
        }

        public static readonly int EBO_ID;
        public static int MaxAmountIndices;
    }

    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    public class LinearVertexBuffer<T> : VertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly OpenGLRenderer renderer;
        private readonly int amountVertices;

        internal LinearVertexBuffer(OpenGLRenderer renderer, int amountVertices, PrimitiveType type, BufferUsageHint usage)
            : base(renderer, amountVertices, usage)
        {
            this.renderer = renderer;
            this.amountVertices = amountVertices;
            Type = type;
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountVertices > LinearIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountVertices];

                for (ushort i = 0; i < amountVertices; i++)
                    indices[i] = i;

                renderer.BindBuffer(BufferTarget.ElementArrayBuffer, LinearIndexData.EBO_ID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(amountVertices * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                LinearIndexData.MaxAmountIndices = amountVertices;
            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                renderer.BindBuffer(BufferTarget.ElementArrayBuffer, LinearIndexData.EBO_ID);
        }

        protected override PrimitiveType Type { get; }
    }
}
