// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class TextureAtlas
    {
        // We are adding an extra padding on top of the padding required by
        // mipmap blending in order to support smooth edges without antialiasing which requires
        // inflating texture rectangles.
        internal const int PADDING = (1 << IRenderer.MAX_MIPMAP_LEVELS) * Sprite.MAX_EDGE_SMOOTHNESS;
        internal const int WHITE_PIXEL_SIZE = 1;

        private readonly List<RectangleI> subTextureBounds = new List<RectangleI>();
        private Texture atlasTexture;

        private readonly IRenderer renderer;
        private readonly int atlasWidth;
        private readonly int atlasHeight;

        private int maxFittableWidth => atlasWidth - PADDING * 2;
        private int maxFittableHeight => atlasHeight - PADDING * 2;

        private Vector2I currentPosition;

        internal TextureWhitePixel WhitePixel
        {
            get
            {
                if (atlasTexture == null)
                    Reset();

                return new TextureWhitePixel(atlasTexture);
            }
        }

        private readonly bool manualMipmaps;
        private readonly All filteringMode;
        private readonly object textureRetrievalLock = new object();

        public TextureAtlas(IRenderer renderer, int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
        {
            this.renderer = renderer;
            atlasWidth = width;
            atlasHeight = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
        }

        private int exceedCount;

        public void Reset()
        {
            subTextureBounds.Clear();
            currentPosition = Vector2I.Zero;

            // We pass PADDING/2 as opposed to PADDING such that the padded region of each individual texture
            // occupies half of the padded space.
            atlasTexture = new AtlasTexture(renderer, atlasWidth, atlasHeight, manualMipmaps, filteringMode, PADDING / 2);

            RectangleI bounds = new RectangleI(0, 0, WHITE_PIXEL_SIZE, WHITE_PIXEL_SIZE);
            subTextureBounds.Add(bounds);

            using (var whiteTex = new TextureSub(atlasTexture, bounds, WrapMode.Repeat, WrapMode.Repeat))
                // Generate white padding as if WhitePixel was wrapped, even though it isn't
                whiteTex.SetData(new TextureUpload(new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, whiteTex.Width, whiteTex.Height, new Rgba32(Vector4.One))));

            currentPosition = new Vector2I(PADDING + WHITE_PIXEL_SIZE, PADDING);
        }

        /// <summary>
        /// Add (allocate) a new texture in the atlas.
        /// </summary>
        /// <param name="width">The width of the requested texture.</param>
        /// <param name="height">The height of the requested texture.</param>
        /// <param name="wrapModeS">The horizontal wrap mode of the texture.</param>
        /// <param name="wrapModeT">The vertical wrap mode of the texture.</param>
        /// <returns>A texture, or null if the requested size exceeds the atlas' bounds.</returns>
        internal Texture Add(int width, int height, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (!canFitEmptyTextureAtlas(width, height)) return null;

            lock (textureRetrievalLock)
            {
                Vector2I position = findPosition(width, height);
                RectangleI bounds = new RectangleI(position.X, position.Y, width, height);
                subTextureBounds.Add(bounds);

                return new TextureAtlasSubTexture(atlasTexture, bounds, wrapModeS, wrapModeT);
            }
        }

        /// <summary>
        /// Whether or not a texture of the given width and height could be placed into a completely empty texture atlas
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>True if the texture could fit an empty texture atlas, false if it could not</returns>
        private bool canFitEmptyTextureAtlas(int width, int height)
        {
            // exceeds bounds in one direction
            if (width > maxFittableWidth || height > maxFittableHeight)
                return false;

            // exceeds bounds in both directions (in this one, we have to account for the white pixel)
            if (width + WHITE_PIXEL_SIZE > maxFittableWidth && height + WHITE_PIXEL_SIZE > maxFittableHeight)
                return false;

            return true;
        }

        /// <summary>
        /// Locates a position in the current texture atlas for a new texture of the given size, or
        /// creates a new texture atlas if there is not enough space in the current one.
        /// </summary>
        /// <param name="width">The width of the requested texture.</param>
        /// <param name="height">The height of the requested texture.</param>
        /// <returns>The position within the texture atlas to place the new texture.</returns>
        private Vector2I findPosition(int width, int height)
        {
            if (atlasTexture == null)
            {
                Logger.Log($"TextureAtlas initialised ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }

            if (currentPosition.Y + height + PADDING > atlasHeight)
            {
                Logger.Log($"TextureAtlas size exceeded {++exceedCount} time(s); generating new texture ({atlasWidth}x{atlasHeight})", LoggingTarget.Performance);
                Reset();
            }

            if (currentPosition.X + width + PADDING > atlasWidth)
            {
                int maxY = 0;

                foreach (RectangleI bounds in subTextureBounds)
                    maxY = Math.Max(maxY, bounds.Bottom + PADDING);

                subTextureBounds.Clear();
                currentPosition = new Vector2I(PADDING, maxY);

                return findPosition(width, height);
            }

            var result = currentPosition;
            currentPosition.X += width + PADDING;

            return result;
        }

        /// <summary>
        /// A TextureGL which is acting as the backing for an atlas.
        /// </summary>
        private class AtlasTexture : Texture
        {
            /// <summary>
            /// The amount of padding around each texture in the atlas.
            /// </summary>
            private readonly int padding;

            private readonly RectangleI atlasBounds;

            private static readonly Rgba32 initialisation_colour = default;

            public AtlasTexture(IRenderer renderer, int width, int height, bool manualMipmaps, All filteringMode = All.Linear, int padding = 0)
                : base(renderer.CreateTexture(width, height, manualMipmaps, filteringMode, initialisationColour: initialisation_colour))
            {
                this.padding = padding;
                atlasBounds = new RectangleI(0, 0, Width, Height);
            }

            internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? opacity)
            {
                // Can only perform padding when the bounds are a sub-part of the texture
                RectangleI middleBounds = upload.Bounds;

                if (middleBounds.IsEmpty || middleBounds.Width * middleBounds.Height > upload.Data.Length)
                {
                    // For a texture atlas, we don't care about opacity, so we avoid
                    // any computations related to it by assuming it to be mixed.
                    base.SetData(upload, wrapModeS, wrapModeT, Opacity.Mixed);
                    return;
                }

                int actualPadding = padding / (1 << upload.Level);

                var data = upload.Data;

                uploadCornerPadding(data, middleBounds, actualPadding, wrapModeS != WrapMode.None && wrapModeT != WrapMode.None);
                uploadHorizontalPadding(data, middleBounds, actualPadding, wrapModeS != WrapMode.None);
                uploadVerticalPadding(data, middleBounds, actualPadding, wrapModeT != WrapMode.None);

                // Upload the middle part of the texture
                // For a texture atlas, we don't care about opacity, so we avoid
                // any computations related to it by assuming it to be mixed.
                base.SetData(upload, wrapModeS, wrapModeT, Opacity.Mixed);
            }

            private void uploadVerticalPadding(ReadOnlySpan<Rgba32> upload, RectangleI middleBounds, int actualPadding, bool fillOpaque)
            {
                RectangleI[] sideBoundsArray =
                {
                    new RectangleI(middleBounds.X, middleBounds.Y - actualPadding, middleBounds.Width, actualPadding).Intersect(atlasBounds), // Top
                    new RectangleI(middleBounds.X, middleBounds.Y + middleBounds.Height, middleBounds.Width, actualPadding).Intersect(atlasBounds), // Bottom
                };

                int[] sideIndices =
                {
                    0, // Top
                    (middleBounds.Height - 1) * middleBounds.Width, // Bottom
                };

                for (int i = 0; i < 2; ++i)
                {
                    RectangleI sideBounds = sideBoundsArray[i];

                    if (!sideBounds.IsEmpty)
                    {
                        bool allTransparent = true;
                        int index = sideIndices[i];

                        var sideUpload = new MemoryAllocatorTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };
                        var data = sideUpload.RawData;

                        for (int y = 0; y < sideBounds.Height; ++y)
                        {
                            for (int x = 0; x < sideBounds.Width; ++x)
                            {
                                Rgba32 pixel = upload[index + x];
                                allTransparent &= checkEdgeRGB(pixel);

                                transferBorderPixel(ref data[y * sideBounds.Width + x], pixel, fillOpaque);
                            }
                        }

                        // Only upload padding if the border isn't completely transparent.
                        if (!allTransparent)
                        {
                            // For a texture atlas, we don't care about opacity, so we avoid
                            // any computations related to it by assuming it to be mixed.
                            base.SetData(sideUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                        }
                    }
                }
            }

            private void uploadHorizontalPadding(ReadOnlySpan<Rgba32> upload, RectangleI middleBounds, int actualPadding, bool fillOpaque)
            {
                RectangleI[] sideBoundsArray =
                {
                    new RectangleI(middleBounds.X - actualPadding, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(atlasBounds), // Left
                    new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y, actualPadding, middleBounds.Height).Intersect(atlasBounds), // Right
                };

                int[] sideIndices =
                {
                    0, // Left
                    middleBounds.Width - 1, // Right
                };

                for (int i = 0; i < 2; ++i)
                {
                    RectangleI sideBounds = sideBoundsArray[i];

                    if (!sideBounds.IsEmpty)
                    {
                        bool allTransparent = true;
                        int index = sideIndices[i];

                        var sideUpload = new MemoryAllocatorTextureUpload(sideBounds.Width, sideBounds.Height) { Bounds = sideBounds };
                        var data = sideUpload.RawData;

                        int stride = middleBounds.Width;

                        for (int y = 0; y < sideBounds.Height; ++y)
                        {
                            for (int x = 0; x < sideBounds.Width; ++x)
                            {
                                Rgba32 pixel = upload[index + y * stride];

                                allTransparent &= checkEdgeRGB(pixel);

                                transferBorderPixel(ref data[y * sideBounds.Width + x], pixel, fillOpaque);
                            }
                        }

                        // Only upload padding if the border isn't completely transparent.
                        if (!allTransparent)
                        {
                            // For a texture atlas, we don't care about opacity, so we avoid
                            // any computations related to it by assuming it to be mixed.
                            base.SetData(sideUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                        }
                    }
                }
            }

            private void uploadCornerPadding(ReadOnlySpan<Rgba32> upload, RectangleI middleBounds, int actualPadding, bool fillOpaque)
            {
                RectangleI[] cornerBoundsArray =
                {
                    new RectangleI(middleBounds.X - actualPadding, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(atlasBounds), // TopLeft
                    new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y - actualPadding, actualPadding, actualPadding).Intersect(atlasBounds), // TopRight
                    new RectangleI(middleBounds.X - actualPadding, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(atlasBounds), // BottomLeft
                    new RectangleI(middleBounds.X + middleBounds.Width, middleBounds.Y + middleBounds.Height, actualPadding, actualPadding).Intersect(atlasBounds), // BottomRight
                };

                int[] cornerIndices =
                {
                    0, // TopLeft
                    middleBounds.Width - 1, // TopRight
                    (middleBounds.Height - 1) * middleBounds.Width, // BottomLeft
                    (middleBounds.Height - 1) * middleBounds.Width + middleBounds.Width - 1, // BottomRight
                };

                for (int i = 0; i < 4; ++i)
                {
                    RectangleI cornerBounds = cornerBoundsArray[i];
                    int nCornerPixels = cornerBounds.Width * cornerBounds.Height;
                    Rgba32 pixel = upload[cornerIndices[i]];

                    // Only upload if we have a non-zero size and if the colour isn't already transparent white
                    if (nCornerPixels > 0 && !checkEdgeRGB(pixel))
                    {
                        var cornerUpload = new MemoryAllocatorTextureUpload(cornerBounds.Width, cornerBounds.Height) { Bounds = cornerBounds };
                        var data = cornerUpload.RawData;

                        for (int j = 0; j < nCornerPixels; ++j)
                            transferBorderPixel(ref data[j], pixel, fillOpaque);

                        // For a texture atlas, we don't care about opacity, so we avoid
                        // any computations related to it by assuming it to be mixed.
                        base.SetData(cornerUpload, WrapMode.None, WrapMode.None, Opacity.Mixed);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void transferBorderPixel(ref Rgba32 dest, Rgba32 source, bool fillOpaque)
            {
                dest.R = source.R;
                dest.G = source.G;
                dest.B = source.B;
                dest.A = fillOpaque ? source.A : (byte)0;
            }

            /// <summary>
            /// Check whether the provided upload edge pixel's RGB components match the initialisation colour.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool checkEdgeRGB(Rgba32 cornerPixel)
                => cornerPixel.R == initialisation_colour.R
                   && cornerPixel.G == initialisation_colour.G
                   && cornerPixel.B == initialisation_colour.B;
        }
    }
}
