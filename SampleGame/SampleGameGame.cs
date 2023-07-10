// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box outerMaskingDisplay;
        private Box innerMaskingDisplay;
        private Box combinedMaskingDisplay;

        private Drawable outerMaskingContainer;
        private Drawable innerMaskingContainer;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Add(new BufferedContainer
            // {
            //     Anchor = Anchor.Centre,
            //     Origin = Anchor.Centre,
            //     RelativeSizeAxes = Axes.Both,
            //     Size = new Vector2(0.5f),
            //     Masking = true,
            //     Rotation = 45f,
            //     Child = new Box
            //     {
            //         RelativeSizeAxes = Axes.Both
            //     }
            // });

            // Add(new BufferedContainer
            // {
            //     Anchor = Anchor.Centre,
            //     Origin = Anchor.Centre,
            //     RelativeSizeAxes = Axes.Both,
            //     Size = new Vector2(0.5f),
            //     Masking = true,
            //     Child = new Container
            //     {
            //         Anchor = Anchor.Centre,
            //         Origin = Anchor.Centre,
            //         RelativeSizeAxes = Axes.Both,
            //         Size = new Vector2(2),
            //         Masking = true,
            //         Name = "A",
            //         Child = new Box
            //         {
            //             RelativeSizeAxes = Axes.Both
            //         }
            //     }
            // });

            Add(outerMaskingContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Width = 0.75f,
                Masking = true,
                Children = new[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0.2f
                    },
                    innerMaskingContainer = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Rotation = 45f,
                        Width = 1.5f,
                        Height = 200,
                        Masking = true,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    }
                }
            });

            Add(outerMaskingDisplay = new Box
            {
                Alpha = 0.5f,
                Colour = Color4.Green
            });

            Add(innerMaskingDisplay = new Box
            {
                Alpha = 0.5f,
                Colour = Color4.Red
            });

            Add(combinedMaskingDisplay = new Box
            {
                Alpha = 0.5f,
                Colour = Color4.Blue
            });
        }

        protected override void Update()
        {
            base.Update();

            outerMaskingDisplay.Position = outerMaskingContainer.ScreenSpaceDrawQuad.AABB.Location;
            outerMaskingDisplay.Size = outerMaskingContainer.ScreenSpaceDrawQuad.AABB.Size;

            innerMaskingDisplay.Position = innerMaskingContainer.ScreenSpaceDrawQuad.AABB.Location;
            innerMaskingDisplay.Size = innerMaskingContainer.ScreenSpaceDrawQuad.AABB.Size;

            RectangleI combinedScreenSpaceMaskingRect = outerMaskingContainer.ScreenSpaceDrawQuad.AABB.Intersect(innerMaskingContainer.ScreenSpaceDrawQuad.AABB);
            Quad q = Quad.FromRectangle(combinedScreenSpaceMaskingRect) * innerMaskingContainer.DrawInfo.MatrixInverse;
            RectangleF scissorAABB = new RectangleF(q.TopLeft, q.BottomRight - q.TopLeft);

            combinedMaskingDisplay.Position = innerMaskingContainer.ToScreenSpace(scissorAABB.Location);
            combinedMaskingDisplay.Size = innerMaskingContainer.ToScreenSpace(scissorAABB.BottomRight) - innerMaskingContainer.ToScreenSpace(scissorAABB.TopLeft);
        }
    }
}
