// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Veldrid
{
    internal interface IVeldridDevice
    {
        GraphicsDevice Device { get; }
        ResourceFactory Factory { get; }
        GraphicsSurfaceType SurfaceType { get; }
        bool VerticalSync { get; set; }
        bool AllowTearing { get; set; }
        bool IsDepthRangeZeroToOne { get; }
        bool IsUvOriginTopLeft { get; }
        bool IsClipSpaceYInverted { get; }
        bool UseStructuredBuffers { get; }
        int MaxTextureSize { get; }
        void SwapBuffers();
        void WaitUntilIdle();
        void WaitUntilNextFrameReady();
        void MakeCurrent();
        void ClearCurrent();
        Image<Rgba32> TakeScreenshot();
    }
}
