// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// Internal interface for all vertex buffers.
    /// </summary>
    public interface IVertexBuffer
    {
        /// <summary>
        /// The <see cref="IRenderer.ResetId"/> when this <see cref="IVertexBuffer"/> was last used.
        /// </summary>
        ulong LastUseResetId { get; }

        /// <summary>
        /// Whether this <see cref="IVertexBuffer"/> is currently in use.
        /// </summary>
        bool InUse { get; }

        /// <summary>
        /// Frees all resources allocated by this <see cref="IVertexBuffer"/>.
        /// </summary>
        void Free();
    }

    public interface IVertexBuffer<in T> : IVertexBuffer, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        /// <summary>
        /// Gets the number of vertices in this <see cref="IVertexBuffer"/>.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="IVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        bool SetVertex(int vertexIndex, T vertex);

        void DrawRange(int startIndex, int endIndex);
        void UpdateRange(int startIndex, int endIndex);
    }
}
