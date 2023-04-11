// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        public static float DegreesToRadians(float degrees)
        {
            return degrees * MathF.PI / 180.0f;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        public static float RadiansToDegrees(float radians)
        {
            return radians * (180.0f / MathF.PI);
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        public static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        public static Vector2 LargestInscribedRectangle(Vector2 size, float radians)
        {
            bool widthIsLonger = size.X >= size.Y;
            float sideLong = widthIsLonger ? size.X : size.Y;
            float sideShort = widthIsLonger ? size.Y : size.X;

            float sinA = MathF.Abs(MathF.Sin(radians));
            float cosA = MathF.Abs(MathF.Cos(radians));

            if (sideShort <= 2 * sinA * cosA * sideLong || MathF.Abs(sinA - cosA) < 1e-6)
            {
                float x = 0.5f * sideShort;

                return widthIsLonger
                    ? new Vector2(x / sinA, x / cosA)
                    : new Vector2(x / cosA, x / sinA);
            }

            float cos2A = cosA * cosA - sinA * sinA;

            return new Vector2(
                (size.X * cosA - size.Y * sinA) / cos2A,
                (size.Y * cosA - size.X * sinA) / cos2A);
        }
    }
}
