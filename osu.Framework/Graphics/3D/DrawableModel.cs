// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics._3D
{
    public partial class DrawableModel : Model
    {
        private readonly Drawable drawable;

        public DrawableModel(Drawable drawable)
        {
            this.drawable = drawable;
        }
    }
}
