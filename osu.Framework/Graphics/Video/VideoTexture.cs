﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Video
{
    internal unsafe class VideoTexture : TextureGLSingle
    {
        private int[] textureIds;

        /// <summary>
        /// Whether the latest frame data has been uploaded.
        /// </summary>
        public bool UploadComplete { get; private set; }

        public VideoTexture(IRenderer renderer, int width, int height, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
            : base(renderer, width, height, true, All.Linear, wrapModeS, wrapModeT)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (uploadOpacity != null && uploadOpacity != Opacity.Opaque)
                throw new InvalidOperationException("Video texture uploads must always be opaque");

            UploadComplete = false;

            // We do not support videos with transparency at this point,
            // so the upload's opacity as well as the texture's opacity
            // is always opaque.
            base.SetData(upload, wrapModeS, wrapModeT, Opacity = Opacity.Opaque);
        }

        public override int TextureId => textureIds?[0] ?? 0;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        internal override bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureIds == null)
                return false;

            bool anyBound = false;

            for (int i = 0; i < textureIds.Length; i++)
                anyBound |= Renderer.BindTexture(textureIds[i], unit + i, wrapModeS, wrapModeT);

            if (anyBound)
                BindCount++;

            return true;
        }

        protected override void DoUpload(ITextureUpload upload, IntPtr dataPointer)
        {
            if (!(upload is VideoTextureUpload videoUpload))
                return;

            // Do we need to generate a new texture?
            if (textureIds == null)
            {
                Debug.Assert(memoryLease == null);
                memoryLease = NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);

                textureIds = new int[3];
                GL.GenTextures(textureIds.Length, textureIds);

                for (uint i = 0; i < textureIds.Length; i++)
                {
                    Renderer.BindTexture(textureIds[i]);

                    int width = videoUpload.GetPlaneWidth(i);
                    int height = videoUpload.GetPlaneHeight(i);

                    textureSize += width * height;

                    GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.R8, width, height,
                        0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }
            }

            for (uint i = 0; i < textureIds.Length; i++)
            {
                Renderer.BindTexture(textureIds[i]);

                GL.PixelStore(PixelStoreParameter.UnpackRowLength, videoUpload.Frame->linesize[i]);

                GL.TexSubImage2D(TextureTarget2d.Texture2D, 0, 0, 0, videoUpload.GetPlaneWidth(i), videoUpload.GetPlaneHeight(i),
                    PixelFormat.Red, PixelType.UnsignedByte, (IntPtr)videoUpload.Frame->data[i]);
            }

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);

            UploadComplete = true;
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            memoryLease?.Dispose();

            Renderer.ScheduleDisposal(v =>
            {
                int[] ids = v.textureIds;

                if (ids == null)
                    return;

                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] >= 0)
                        GL.DeleteTextures(1, new[] { ids[i] });
                }
            }, this);
        }

        #endregion
    }
}
