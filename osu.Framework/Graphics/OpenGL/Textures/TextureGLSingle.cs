// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Extensions.ImageExtensions;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class TextureGLSingle : TextureGL
    {
        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        private int internalWidth;
        private int internalHeight;

        private readonly All filteringMode;

        private readonly Rgba32 initialisationColour;

        /// <summary>
        /// The total amount of times this <see cref="TextureGLSingle"/> was bound.
        /// </summary>
        public ulong BindCount { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="TextureGLSingle"/>.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public TextureGLSingle(OpenGLRenderer renderer, int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, Rgba32 initialisationColour = default)
            : base(renderer)
        {
            Width = width;
            Height = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;
        }

        #region Disposal

        ~TextureGLSingle()
        {
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

            Renderer.ScheduleDisposal(texture =>
            {
                int disposableId = texture.textureId;

                if (disposableId <= 0)
                    return;

                GL.DeleteTextures(1, new[] { disposableId });

                texture.memoryLease?.Dispose();

                texture.textureId = 0;
            }, this);
        }

        #endregion

        #region Memory Tracking

        private List<long> levelMemoryUsage = new List<long>();

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private void updateMemoryUsage(int level, long newUsage)
        {
            levelMemoryUsage ??= new List<long>();

            while (level >= levelMemoryUsage.Count)
                levelMemoryUsage.Add(0);

            levelMemoryUsage[level] = newUsage;

            memoryLease?.Dispose();
            memoryLease = NativeMemoryTracker.AddMemory(this, getMemoryUsage());
        }

        private long getMemoryUsage()
        {
            long usage = 0;

            for (int i = 0; i < levelMemoryUsage.Count; i++)
                usage += levelMemoryUsage[i];

            return usage;
        }

        #endregion

        private int height;

        public override int Height
        {
            get => height;
            set => height = value;
        }

        private int width;

        public override int Width
        {
            get => width;
            set => width = value;
        }

        private int textureId;

        public override int TextureId
        {
            get
            {
                if (!Available)
                    throw new ObjectDisposedException(ToString(), "Can not obtain ID of a disposed texture.");

                if (textureId == 0)
                    throw new InvalidOperationException("Can not obtain ID of a texture before uploading it.");

                return textureId;
            }
        }

        public override void SetData(ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                if (requireUpload && !BypassTextureUploadQueueing)
                    Renderer.EnqueueTextureUpload(this);
            }
        }

        internal override bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureId <= 0)
                return false;

            if (Renderer.BindTexture(textureId, unit, wrapModeS, wrapModeT))
                BindCount++;

            return true;
        }

        private bool manualMipmaps;

        internal override unsafe bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw OGL calls on another thread than the main thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            bool didUpload = false;

            while (tryGetNextUpload(out ITextureUpload upload))
            {
                using (upload)
                {
                    fixed (Rgba32* ptr = upload.Data)
                        DoUpload(upload, (IntPtr)ptr);

                    didUpload = true;
                }
            }

            if (didUpload && !manualMipmaps)
            {
                GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest);
                GL.GenerateMipmap(TextureTarget.Texture2D);
            }

            return didUpload;
        }

        public override bool UploadComplete
        {
            get
            {
                lock (uploadQueue)
                    return uploadQueue.Count == 0;
            }
        }

        protected override void FlushUploads()
        {
            while (tryGetNextUpload(out var upload))
                upload.Dispose();
        }

        private bool tryGetNextUpload(out ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                if (uploadQueue.Count == 0)
                {
                    upload = null;
                    return false;
                }

                upload = uploadQueue.Dequeue();
                return true;
            }
        }

        protected virtual void DoUpload(ITextureUpload upload, IntPtr dataPointer)
        {
            // Do we need to generate a new texture?
            if (textureId <= 0 || internalWidth != width || internalHeight != height)
            {
                internalWidth = width;
                internalHeight = height;

                // We only need to generate a new texture if we don't have one already. Otherwise just re-use the current one.
                if (textureId <= 0)
                {
                    int[] textures = new int[1];
                    GL.GenTextures(1, textures);

                    textureId = textures[0];

                    Renderer.BindTexture(textureId);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, IRenderer.MAX_MIPMAP_LEVELS);

                    // These shouldn't be required, but on some older Intel drivers the MAX_LOD chosen by the shader isn't clamped to the MAX_LEVEL from above, resulting in disappearing textures.
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, IRenderer.MAX_MIPMAP_LEVELS);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                        (int)(manualMipmaps ? filteringMode : filteringMode == All.Linear ? All.LinearMipmapLinear : All.Nearest));
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filteringMode);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }
                else
                    Renderer.BindTexture(textureId);

                if ((width == upload.Bounds.Width && height == upload.Bounds.Height) || dataPointer == IntPtr.Zero)
                {
                    updateMemoryUsage(upload.Level, (long)width * height * 4);
                    GL.TexImage2D(TextureTarget2d.Texture2D, upload.Level, TextureComponentCount.Srgb8Alpha8, width, height, 0, upload.Format, PixelType.UnsignedByte, dataPointer);
                }
                else
                {
                    initializeLevel(upload.Level, width, height);

                    GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format,
                        PixelType.UnsignedByte, dataPointer);
                }
            }
            // Just update content of the current texture
            else if (dataPointer != IntPtr.Zero)
            {
                Renderer.BindTexture(textureId);

                if (!manualMipmaps && upload.Level > 0)
                {
                    //allocate mipmap levels
                    int level = 1;
                    int d = 2;

                    while (width / d > 0)
                    {
                        initializeLevel(level, width / d, height / d);
                        level++;
                        d *= 2;
                    }

                    manualMipmaps = true;
                }

                int div = (int)Math.Pow(2, upload.Level);

                GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X / div, upload.Bounds.Y / div, upload.Bounds.Width / div, upload.Bounds.Height / div,
                    upload.Format, PixelType.UnsignedByte, dataPointer);
            }
        }

        private void initializeLevel(int level, int width, int height)
        {
            using (var image = createBackingImage(width, height))
            using (var pixels = image.CreateReadOnlyPixelSpan())
            {
                updateMemoryUsage(level, (long)width * height * 4);
                GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Srgb8Alpha8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
                    ref MemoryMarshal.GetReference(pixels.Span));
            }
        }

        private Image<Rgba32> createBackingImage(int width, int height)
        {
            // it is faster to initialise without a background specification if transparent black is all that's required.
            return initialisationColour == default
                ? new Image<Rgba32>(width, height)
                : new Image<Rgba32>(width, height, initialisationColour);
        }
    }
}
