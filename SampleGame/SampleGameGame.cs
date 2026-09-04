// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRange(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White
                },
                box = new Box
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(30),
                    Colour = Color4.Tomato
                }
            });

            Host.Window.CursorState = CursorState.Default;
        }

        protected override void Update()
        {
            base.Update();

            box.Position = ToLocalSpace(GetContainingInputManager()!.CurrentState.Mouse.Position);
        }
    }
}
