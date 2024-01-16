// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using Veldrid;

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

        void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data) where T : unmanaged;

        void UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes);

        CommandList BufferUpdateCommands { get; }

        void EnqueueTextureUpload(VeldridTexture texture);

        void GenerateMipmaps(VeldridTexture texture);
    }
}
