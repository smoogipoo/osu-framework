// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTexture : ITexture
    {
        /// <summary>
        /// Contains all currently-active <see cref="VeldridTexture"/>s.
        /// </summary>
        private static readonly LockedWeakList<VeldridTexture> all_textures = new LockedWeakList<VeldridTexture>();

        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        /// <summary>
        /// Invoked when a new <see cref="VeldridTexture"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<VeldridTexture> TextureCreated;

        private readonly All filteringMode;

        private readonly Rgba32 initialisationColour;

        /// <summary>
        /// The total amount of times this <see cref="VeldridTexture"/> was bound.
        /// </summary>
        public ulong BindCount { get; protected set; }

        public RectangleI Bounds => new RectangleI(0, 0, Width, Height);

        protected virtual TextureUsage Usages
        {
            get
            {
                var usages = TextureUsage.Sampled;

                if (!manualMipmaps)
                    usages |= TextureUsage.GenerateMipmaps;

                return usages;
            }
        }

        private readonly VeldridRenderer renderer;

        /// <summary>
        /// Creates a new <see cref="VeldridTexture"/>.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public VeldridTexture(VeldridRenderer renderer, int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear,
                              WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
        {
            this.renderer = renderer;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;

            Width = width;
            Height = height;

            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;

            all_textures.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="VeldridTexture"/>s.
        /// </summary>
        public static VeldridTexture[] GetAllTextures() => all_textures.ToArray();

        #region Disposal

        ~VeldridTexture()
        {
            Dispose(false);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            all_textures.Remove(this);

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

            renderer.ScheduleDisposal(t =>
            {
                t.Available = false;

                if (t.texture == null)
                    return;

                t.memoryLease?.Dispose();

                t.resource?.Dispose();
                t.resource = null;

                t.sampler?.Dispose();
                t.sampler = null;

                t.texture?.Dispose();
                t.texture = null;
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

        public bool Available { get; private set; }
        public bool BypassTextureUploadQueueing { get; set; }

        bool ITexture.IsQueuedForUpload { get; set; }

        public Opacity Opacity { get; private set; }

        public int MaxSize => renderer.MaxTextureSize;

        public WrapMode WrapModeS { get; }
        public WrapMode WrapModeT { get; }

        public int Width { get; set; }
        public int Height { get; set; }

        private Texture texture;
        private Sampler sampler;

        private VeldridTextureSamplerSet resource;

        public VeldridTextureSamplerSet Resource
        {
            get
            {
                if (!Available)
                    throw new ObjectDisposedException(ToString(), "Can not obtain resource set of a disposed texture.");

                if (resource == null)
                    throw new InvalidOperationException("Can not obtain resource set of a texture before uploading it.");

                return resource;
            }
        }

        /// <summary>
        /// Retrieves the size of this texture in bytes.
        /// </summary>
        public virtual int GetByteSize() => Width * Height * 4;

        private static void rotateVector(ref Vector2 toRotate, float sin, float cos)
        {
            float oldX = toRotate.X;
            toRotate.X = toRotate.X * cos - toRotate.Y * sin;
            toRotate.Y = oldX * sin + toRotate.Y * cos;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect)
        {
            RectangleF texRect = textureRect != null
                ? new RectangleF(textureRect.Value.X, textureRect.Value.Y, textureRect.Value.Width, textureRect.Value.Height)
                : new RectangleF(0, 0, Width, Height);

            texRect.X /= Width;
            texRect.Y /= Height;
            texRect.Width /= Width;
            texRect.Height /= Height;

            return texRect;
        }

        public const int VERTICES_PER_TRIANGLE = 4;

        public const int VERTICES_PER_QUAD = 4;

        public void SetData(ITextureUpload upload)
        {
            throw new NotImplementedException();
        }

        void ITexture.SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            SetData(upload, wrapModeS, wrapModeT, uploadOpacity);
        }

        internal virtual void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.IsEmpty && upload.Data.Length > 0)
            {
                upload.Bounds = Bounds;
                if (Width * Height > upload.Data.Length)
                    throw new InvalidOperationException($"Size of texture upload ({Width}x{Height}) does not contain enough data ({upload.Data.Length} < {Width * Height})");
            }

            UpdateOpacity(upload, ref uploadOpacity);

            lock (uploadQueue)
            {
                // bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                // todo: support enqueuing texture uploads in IRenderer
                // if (requireUpload && !BypassTextureUploadQueueing)
                // renderer.EnqueueTextureUpload(this);
            }
        }

        protected static Opacity ComputeOpacity(ITextureUpload upload)
        {
            // TODO: Investigate performance issues and revert functionality once we are sure there is no overhead.
            // see https://github.com/ppy/osu/issues/9307
            return Opacity.Mixed;

            // ReadOnlySpan<Rgba32> data = upload.Data;
            //
            // if (data.Length == 0)
            //     return Opacity.Transparent;
            //
            // int firstPixelValue = data[0].A;
            //
            // // Check if the first pixel has partial transparency (neither fully-opaque nor fully-transparent).
            // if (firstPixelValue != 0 && firstPixelValue != 255)
            //     return Opacity.Mixed;
            //
            // // The first pixel is GUARANTEED to be either fully-opaque or fully-transparent.
            // // Now we need to go through the rest of the image and check that every other pixel matches this value.
            // for (int i = 1; i < data.Length; i++)
            // {
            //     if (data[i].A != firstPixelValue)
            //         return Opacity.Mixed;
            // }
            //
            // return firstPixelValue == 0 ? Opacity.Transparent : Opacity.Opaque;
        }

        protected void UpdateOpacity(ITextureUpload upload, ref Opacity? uploadOpacity)
        {
            // Compute opacity if it doesn't have a value yet
            uploadOpacity ??= ComputeOpacity(upload);

            // Update the texture's opacity depending on the upload's opacity.
            // If the upload covers the entire bounds of the texture, it fully
            // determines the texture's opacity. Otherwise, it can only turn
            // the texture's opacity into a mixed state (if it disagrees with
            // the texture's existing opacity).
            if (upload.Bounds == Bounds && upload.Level == 0)
                Opacity = uploadOpacity.Value;
            else if (uploadOpacity.Value != Opacity)
                Opacity = Opacity.Mixed;
        }

        /// <summary>
        /// Whether this <see cref="VeldridTexture"/> generates mipmaps manually.
        /// </summary>
        private readonly bool manualMipmaps;

        bool ITexture.Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (resource == null)
                return false;

            if (renderer.BindTexture(resource, wrapModeS, wrapModeT))
                BindCount++;

            return true;
        }

        internal bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw Veldrid calls on another thread than the draw thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            bool didUpload = false;

            while (tryGetNextUpload(out ITextureUpload upload))
            {
                using (upload)
                {
                    DoUpload(upload);
                    didUpload = true;
                }
            }

            if (didUpload && !(manualMipmaps || maximumUploadedLod > 0))
                renderer.Commands.GenerateMipmaps(texture);

            return didUpload;
        }

        bool ITexture.Upload() => Upload();

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

        /// <summary>
        /// The maximum number of mip levels provided by a <see cref="ITextureUpload"/>.
        /// </summary>
        /// <remarks>
        /// This excludes automatic generation of mipmaps via the graphics backend.
        /// </remarks>
        private int maximumUploadedLod;

        protected virtual void DoUpload(ITextureUpload upload)
        {
            if (texture == null || texture.Width != Width || texture.Height != Height)
            {
                resource?.Dispose();
                resource = null;

                sampler?.Dispose();
                sampler = null;

                texture?.Dispose();

                var textureDescription = TextureDescription.Texture2D((uint)Width, (uint)Height, (uint)calculateMipmapLevels(Width, Height), 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, Usages);
                texture = renderer.Factory.CreateTexture(textureDescription);

                // todo: we should probably look into not having to allocate enough data for initialising textures
                // similar to how OpenGL allows calling glTexImage2D with null data pointer.
                initialiseLevel(0, Width, Height);

                maximumUploadedLod = 0;
            }

            int lastMaximumUploadedLod = maximumUploadedLod;

            if (!upload.Data.IsEmpty)
            {
                // ensure all mip levels up to the target level are initialised.
                if (upload.Level > maximumUploadedLod)
                {
                    for (int i = maximumUploadedLod + 1; i <= upload.Level; i++)
                        initialiseLevel(i, Width >> i, Height >> i);

                    maximumUploadedLod = upload.Level;
                }

                renderer.UpdateTexture(texture, upload.Bounds.X >> upload.Level, upload.Bounds.Y >> upload.Level, upload.Bounds.Width >> upload.Level, upload.Bounds.Height >> upload.Level, upload.Level, upload.Data);
            }

            if (sampler == null || maximumUploadedLod > lastMaximumUploadedLod)
            {
                resource?.Dispose();
                resource = null;

                sampler?.Dispose();

                bool useUploadMipmaps = manualMipmaps || maximumUploadedLod > 0;

                var samplerDescription = new SamplerDescription
                {
                    AddressModeU = SamplerAddressMode.Clamp,
                    AddressModeV = SamplerAddressMode.Clamp,
                    AddressModeW = SamplerAddressMode.Clamp,
                    Filter = filteringMode.ToSamplerFilter(),
                    MinimumLod = 0,
                    MaximumLod = useUploadMipmaps ? (uint)maximumUploadedLod : IRenderer.MAX_MIPMAP_LEVELS,
                    MaximumAnisotropy = 0,
                };

                sampler = renderer.Factory.CreateSampler(samplerDescription);
            }

            resource ??= new VeldridTextureSamplerSet(renderer, texture, sampler);
        }

        private unsafe void initialiseLevel(int level, int width, int height)
        {
            using (var image = createBackingImage(width, height))
            using (var pixels = image.CreateReadOnlyPixelSpan())
            {
                updateMemoryUsage(level, (long)width * height * sizeof(Rgba32));
                renderer.UpdateTexture(texture, 0, 0, width, height, level, pixels.Span);
            }
        }

        private Image<Rgba32> createBackingImage(int width, int height)
        {
            // it is faster to initialise without a background specification if transparent black is all that's required.
            return initialisationColour == default
                ? new Image<Rgba32>(width, height)
                : new Image<Rgba32>(width, height, initialisationColour);
        }

        // todo: should this be limited to MAX_MIPMAP_LEVELS or was that constant supposed to be for automatic mipmap generation only?
        // previous implementation was allocating mip levels all the way to 1x1 size when an ITextureUpload.Level > 0, therefore it's not limited there.
        private static int calculateMipmapLevels(int width, int height) => 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
}
