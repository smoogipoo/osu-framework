// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Veldrid
{
    /// <summary>
    /// Basic interface for all renderers that need Veldrid graphics objects.
    /// </summary>
    internal interface IVeldridRenderer : IRenderer
    {
        GraphicsDevice Device { get; }

        ResourceFactory Factory { get; }

        GraphicsSurfaceType SurfaceType { get; }

        bool UseStructuredBuffers { get; }

        CommandList BufferUpdateCommands { get; }

        void BindShader(VeldridShader shader);

        void UnbindShader(VeldridShader shader);

        void BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer);

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        void UpdateTexture<T>(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data) where T : unmanaged;

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <param name="rowLengthInBytes">The number of bytes per row of the image to read from <paramref name="data"/>.</param>
        void UpdateTexture(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes);

        void EnqueueTextureUpload(VeldridTexture texture);

        void GenerateMipmaps(VeldridTexture texture);

        void BindFrameBuffer(VeldridFrameBuffer frameBuffer);

        void UnbindFrameBuffer(VeldridFrameBuffer frameBuffer);

        void RegisterUniformBufferForReset(IVeldridUniformBuffer veldridUniformBuffer);

        Texture CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None);
    }
}
