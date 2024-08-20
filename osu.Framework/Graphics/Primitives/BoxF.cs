// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoxF : IEquatable<BoxF>
    {
        /// <summary>Represents an instance of the <see cref="BoxF"/> class with its members uninitialized.</summary>
        /// <filterpriority>1</filterpriority>
        public static BoxF Empty { get; } = new BoxF();

        public float X;
        public float Y;
        public float Z;

        public float Width;
        public float Height;
        public float Depth;

        /// <summary>Initializes a new instance of the <see cref="BoxF"/> class with the specified location and size.</summary>
        /// <param name="x">The x-coordinate of the front-upper-left corner of the box.</param>
        /// <param name="y">The y-coordinate of the front-upper-left corner of the box.</param>
        /// <param name="z">The z-coordinate of the front of the box.</param>
        /// <param name="width">The width of the box.</param>
        /// <param name="height">The height of the box.</param>
        /// <param name="depth">The depth of the box.</param>
        public BoxF(float x, float y, float z, float width, float height, float depth)
        {
            X = x;
            Y = y;
            Z = z;
            Width = width;
            Height = height;
            Depth = depth;
        }

        /// <summary>Initializes a new instance of the <see cref="BoxF"/> class with the specified location and size.</summary>
        /// <param name="location">A <see cref="Vector2"/> that represents the front-upper-left corner of the box.</param>
        /// <param name="size">A <see cref="Vector3"/> that represents the width, height, and depth of the box.</param>
        public BoxF(Vector3 location, Vector3 size)
        {
            X = location.X;
            Y = location.Y;
            Z = location.Z;
            Width = size.X;
            Height = size.Y;
            Depth = size.Z;
        }

        /// <summary>Gets or sets the coordinates of the front-upper-left corner of this <see cref="BoxF"/> structure.</summary>
        /// <returns>A <see cref="Vector3"/> that represents the front-upper-left corner of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector3 Location
        {
            get => new Vector3(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        /// <summary>Gets or sets the size of this <see cref="BoxF"/>.</summary>
        /// <returns>A <see cref="Vector3"/> that represents the width, height, and depth of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector3 Size
        {
            get => new Vector3(Width, Height, Depth);
            set
            {
                Width = value.X;
                Height = value.Y;
                Depth = value.Z;
            }
        }

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Left => X;

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Top => Y;

        /// <summary>Gets the x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Right => X + Width;

        /// <summary>Gets the y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Bottom => Y + Height;

        /// <summary>Gets the z-coordinate of the front plane of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The z-coordinate of the front plane of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public float Front => Z;

        /// <summary>Gets the z-coordinate that is the sum of <see cref="Z"/> and <see cref="Depth"/> of this <see cref="BoxF"/> structure.</summary>
        /// <returns>The z-coordinate that is the sum of <see cref="Z"/> and <see cref="Depth"/> of this <see cref="BoxF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        public float Back => Z + Depth;

        /// <summary>Gets the front-top-left corner of the box.</summary>
        [Browsable(false)]
        public Vector3 FrontTopLeft => new Vector3(Left, Top, Front);

        /// <summary>Gets the front-top-right corner of the box.</summary>
        [Browsable(false)]
        public Vector3 FrontTopRight => new Vector3(Right, Top, Front);

        /// <summary>Gets the front-bottom-left corner of the box.</summary>
        [Browsable(false)]
        public Vector3 FrontBottomLeft => new Vector3(Left, Bottom, Front);

        /// <summary>Gets the front-bottom-right corner of the box.</summary>
        [Browsable(false)]
        public Vector3 FrontBottomRight => new Vector3(Right, Bottom, Front);

        /// <summary>Gets the front-top-left corner of the box.</summary>
        [Browsable(false)]
        public Vector3 BackTopLeft => new Vector3(Left, Top, Back);

        /// <summary>Gets the front-top-right corner of the box.</summary>
        [Browsable(false)]
        public Vector3 BackTopRight => new Vector3(Right, Top, Back);

        /// <summary>Gets the front-bottom-left corner of the box.</summary>
        [Browsable(false)]
        public Vector3 BackBottomLeft => new Vector3(Left, Bottom, Back);

        /// <summary>Gets the front-bottom-right corner of the box.</summary>
        [Browsable(false)]
        public Vector3 BackBottomRight => new Vector3(Right, Bottom, Back);

        /// <summary>Gets the center of the box.</summary>
        [Browsable(false)]
        public Vector3 Centre => new Vector3(X + Width / 2, Y + Height / 2, Z + Depth / 2);

        /// <summary>Tests whether the <see cref="Width"/>, <see cref="Height"/>, or <see cref="Depth"/> property of this <see cref="BoxF"/> has a value of zero.</summary>
        /// <returns>This property returns true if the <see cref="Width"/>, <see cref="Height"/>, or <see cref="Depth"/> property of this <see cref="BoxF"/> has a value of zero; otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public bool IsEmpty => Width <= 0 || Height <= 0 || Depth <= 0;

        public bool Equals(BoxF other)
        {
            return X.Equals(other.X)
                   && Y.Equals(other.Y)
                   && Z.Equals(other.Z)
                   && Width.Equals(other.Width)
                   && Height.Equals(other.Height)
                   && Depth.Equals(other.Depth);
        }

        public override bool Equals(object? obj)
        {
            return obj is BoxF other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, Width, Height, Depth);
        }
    }
}
