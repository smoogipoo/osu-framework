// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class Wangs : TestScene
    {
        public Wangs()
        {
            var m = Matrix3.Identity;

            MatrixExtensions.PerspectiveFromLeft(ref m, new Vector2(2, 4));
        }
    }
}
