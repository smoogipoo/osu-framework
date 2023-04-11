// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Masking = true,
                Rotation = 45f,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Green
                    },
                    box = new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Tomato,
                        Rotation = -45f
                    }
                }
            });
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}
