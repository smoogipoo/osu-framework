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
    internal interface IVeldridRenderer : IRenderer
    {
        GraphicsDevice Device { get; }

        ResourceFactory Factory { get; }

        GraphicsSurfaceType SurfaceType { get; }

        bool UseStructuredBuffers { get; }

        void BindShader(VeldridShader shader);

        void UnbindShader(VeldridShader shader);

        void BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer);

        void UpdateTexture<T>(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data) where T : unmanaged;

        void UpdateTexture(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes);

        CommandList BufferUpdateCommands { get; }

        void EnqueueTextureUpload(VeldridTexture texture);

        void GenerateMipmaps(VeldridTexture texture);

        void BindFrameBuffer(VeldridFrameBuffer frameBuffer);

        void UnbindFrameBuffer(VeldridFrameBuffer frameBuffer);

        void DeleteFrameBuffer(VeldridFrameBuffer frameBuffer);

        bool IsFrameBufferBound(VeldridFrameBuffer frameBuffer);

        Texture CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None);

        void RegisterUniformBufferForReset(IVeldridUniformBuffer veldridUniformBuffer);
    }
}
