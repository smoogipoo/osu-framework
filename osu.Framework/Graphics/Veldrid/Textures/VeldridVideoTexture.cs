// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal unsafe class VeldridVideoTexture : VeldridTexture
    {
        public global::Veldrid.Texture[]? TextureResources { get; private set; }
        public Sampler[]? SamplerResources { get; private set; }

        public VeldridVideoTexture(VeldridRenderer renderer, int width, int height)
            : base(renderer, width, height, true)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        protected override void DoUpload(ITextureUpload upload)
        {
            if (!(upload is VideoTextureUpload videoUpload))
                return;

            // Do we need to generate a new texture?
            if (TextureResources == null)
            {
                Debug.Assert(memoryLease == null);
                memoryLease = NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);

                TextureResources = new global::Veldrid.Texture[3];
                SamplerResources = new Sampler[3];

                for (uint i = 0; i < TextureResources.Length; i++)
                {
                    int width = videoUpload.GetPlaneWidth(i);
                    int height = videoUpload.GetPlaneHeight(i);
                    int countPixels = width * height;

                    var textureDescription = TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_UNorm, Usages);
                    TextureResources[i] = Renderer.Factory.CreateTexture(ref textureDescription);
                    SamplerResources[i] = Renderer.Factory.CreateSampler(new SamplerDescription
                    {
                        AddressModeU = SamplerAddressMode.Clamp,
                        AddressModeV = SamplerAddressMode.Clamp,
                        AddressModeW = SamplerAddressMode.Clamp,
                        Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
                        MinimumLod = 0,
                        MaximumLod = IRenderer.MAX_MIPMAP_LEVELS,
                        MaximumAnisotropy = 0,
                    });

                    textureSize += countPixels;
                }
            }

            for (uint i = 0; i < TextureResources.Length; i++)
            {
                Renderer.UpdateTexture(
                    TextureResources[i],
                    0,
                    0,
                    videoUpload.GetPlaneWidth(i),
                    videoUpload.GetPlaneHeight(i),
                    0,
                    new IntPtr(videoUpload.Frame->data[i]),
                    videoUpload.Frame->linesize[i]);
            }
        }

        public override IEnumerable<VeldridTextureResource> GetResources() => new[]
        {
            new VeldridTextureResource(TextureResources[0], SamplerResources[0]),
            new VeldridTextureResource(TextureResources[1], SamplerResources[1]),
            new VeldridTextureResource(TextureResources[2], SamplerResources[2]),
        };

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            memoryLease?.Dispose();

            Renderer.ScheduleDisposal(v =>
            {
                // int[]? ids = v.TextureIds;
                //
                // if (ids == null)
                //     return;
                //
                // for (int i = 0; i < ids.Length; i++)
                // {
                //     if (ids[i] >= 0)
                //         GL.DeleteTextures(1, new[] { ids[i] });
                // }
            }, this);
        }

        #endregion
    }
}
