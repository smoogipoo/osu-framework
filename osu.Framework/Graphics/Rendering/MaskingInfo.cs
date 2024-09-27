// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Graphics.Rendering
{
    public struct MaskingInfo : IEquatable<MaskingInfo>
    {
        /// <summary>
        /// The masking rectangle in coordinates relative to the masking space.
        /// </summary>
        public RectangleF MaskingRect;

        /// <summary>
        /// The masking rectangle in screen-space coordinates.
        /// </summary>
        public RectangleF ScreenSpaceMaskingRect;

        /// <summary>
        /// Transforms from screen-space coordinates to the masking space.
        /// </summary>
        public Matrix3 ToMaskingSpace;

        /// <summary>
        /// Transforms from screen-space coordinates to the scissor space.
        /// </summary>
        public Matrix3 ToScissorSpace;

        public Quad ConservativeScreenSpaceQuad;

        public float CornerRadius;
        public float CornerExponent;

        public float BorderThickness;
        public ColourInfo BorderColour;

        public float BlendRange;
        public float AlphaExponent;

        public Vector2 EdgeOffset;

        public bool Hollow;
        public float HollowCornerRadius;

        public readonly bool Equals(MaskingInfo other) => this == other;

        public static bool operator ==(in MaskingInfo left, in MaskingInfo right) =>
            left.MaskingRect == right.MaskingRect &&
            left.ScreenSpaceMaskingRect == right.ScreenSpaceMaskingRect &&
            left.ToMaskingSpace == right.ToMaskingSpace &&
            left.ToScissorSpace == right.ToScissorSpace &&
            left.ConservativeScreenSpaceQuad.Equals(right.ConservativeScreenSpaceQuad) &&
            left.CornerRadius == right.CornerRadius &&
            left.CornerExponent == right.CornerExponent &&
            left.BorderThickness == right.BorderThickness &&
            left.BorderColour.Equals(right.BorderColour) &&
            left.BlendRange == right.BlendRange &&
            left.AlphaExponent == right.AlphaExponent &&
            left.EdgeOffset == right.EdgeOffset &&
            left.Hollow == right.Hollow &&
            left.HollowCornerRadius == right.HollowCornerRadius;

        public static bool operator !=(in MaskingInfo left, in MaskingInfo right) => !(left == right);

        public override readonly bool Equals(object? obj) => obj is MaskingInfo other && this == other;

        public override readonly int GetHashCode() => 0; // Shouldn't be used; simplifying implementation here.
    }
}
