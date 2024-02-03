// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public sealed partial class TestSceneBorderColourPerformance : RepeatedDrawablePerformanceTestScene
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            Flow.Masking = true;
            Flow.BorderThickness = 100;
            Flow.BorderColour = new ColourInfo
            {
                TopLeft = Color4.Red,
                TopRight = Color4.Yellow,
                BottomLeft = Color4.Green,
                BottomRight = Color4.Blue
            };
        }

        protected override Drawable CreateDrawable() => new Box();
    }
}
