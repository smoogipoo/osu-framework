// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneQuadTree : FrameworkTestScene
    {
        private readonly QuadTree<QuadTreeVector2Point> quadTree;
        private readonly Container boxes;
        private readonly Container points;

        public TestSceneQuadTree()
        {
            quadTree = new QuadTree<QuadTreeVector2Point>(new RectangleF(0, 0, 800, 600));

            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(800, 600),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.2f,
                    },
                    boxes = new Container
                    {
                        RelativeSizeAxes = Axes.Both
                    },
                    points = new Container
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                }
            };
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnMouseDown(e);

            Vector2 localPos = points.ToLocalSpace(e.ScreenSpaceMouseDownPosition);

            quadTree.Insert(localPos);

            points.Add(new Circle
            {
                Origin = Anchor.Centre,
                Size = new Vector2(12),
                Colour = Color4.Yellow,
                Position = localPos
            });

            boxes.Clear();

            foreach (var area in quadTree.EnumerateAreas())
            {
                boxes.Add(new Container
                {
                    Position = area.Location,
                    Size = area.Size,
                    Masking = true,
                    BorderColour = Color4.White,
                    BorderThickness = 2,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    }
                });
            }

            return true;
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (!e.IsPressed(MouseButton.Right))
                return base.OnMouseMove(e);

            if (quadTree.TryGetClosest(points.ToLocalSpace(e.ScreenSpaceMousePosition), out QuadTreeVector2Point closest))
            {
                foreach (var p in points)
                    p.Colour = Precision.AlmostEquals(p.Position, closest) ? Color4.Blue : Color4.Yellow;
            }

            return true;
        }
    }
}
