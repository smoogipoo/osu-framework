// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestScenePerspective : TestScene
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            Box box;

            Child = box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300),
            };

            AddSliderStep("Perspective-X", -4f, 4f, 0, x => box.Perspective = new Vector2(x / 100, box.Perspective.Y));
            AddSliderStep("Perspective-Y", -4f, 4f, 0, y => box.Perspective = new Vector2(box.Perspective.X, y / 100));
            AddSliderStep("Scale", 0f, 1f, 1, scale => box.Scale = new Vector2(scale));
        }
    }
}
