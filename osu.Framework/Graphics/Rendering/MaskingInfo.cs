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
        /// A rectangle in screen-space that defines the scissor area.
        /// </summary>
        public RectangleI ScreenSpaceScissorArea;

        /// <summary>
        /// A rectangle in the local-space (i.e. <see cref="Drawable.DrawRectangle"/>) of the masking container, that defines the masking area.
        /// </summary>
        public RectangleF MaskingSpaceArea;

        /// <summary>
        /// A quad representing the internal "safe" (without borders, corners, and AA smoothening) area of the masking container.
        /// </summary>
        /// <remarks>
        /// This is used to clip drawn polygons during the front-to-back pass such that only areas guaranteed to be visible are drawn.
        /// </remarks>
        public Quad ConservativeScreenSpaceQuad;

        /// <summary>
        /// A matrix that converts from vertex coordinates to the space of <see cref="MaskingSpaceArea"/>.
        /// </summary>
        public Matrix3 ToMaskingSpace;

        /// <summary>
        /// A matrix that converts from vertex coordinates to the space of <see cref="ScreenSpaceScissorArea"/>.
        /// </summary>
        public Matrix3 ToScissorSpace;

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
            left.ScreenSpaceScissorArea == right.ScreenSpaceScissorArea &&
            left.MaskingSpaceArea == right.MaskingSpaceArea &&
            left.ConservativeScreenSpaceQuad.Equals(right.ConservativeScreenSpaceQuad) &&
            left.ToMaskingSpace == right.ToMaskingSpace &&
            left.ToScissorSpace == right.ToScissorSpace &&
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
