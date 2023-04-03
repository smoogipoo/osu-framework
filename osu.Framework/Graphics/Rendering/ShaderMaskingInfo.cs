// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Rendering
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ShaderMaskingInfo
    {
        public UniformMatrix3 ToMaskingSpace;
        public UniformBool IsMasking;
        public UniformFloat CornerRadius;
        public UniformFloat CornerExponent;
        private readonly UniformPadding4 pad2;

        public UniformVector4 MaskingRect;
        public UniformFloat BorderThickness;
        private readonly UniformPadding12 pad3;

        public UniformMatrix4 BorderColour;
        public UniformFloat MaskingBlendRange;
        public UniformFloat AlphaExponent;
        public UniformVector2 EdgeOffset;

        public UniformBool DiscardInner;
        public UniformFloat InnerCornerRadius;
        private readonly UniformPadding8 pad4;
    }
}
