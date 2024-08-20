// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics._3D;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Tests.Visual;
using osuTK;

namespace osu.Framework.Tests
{
    public partial class TestScene3D : FrameworkTestScene
    {
        public TestScene3D()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = new World
                {
                    Child = new DrawableModel(new Container
                    {
                        Size = new Vector2(500),
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    })
                    {
                        Rotation = Quaternion.Identity,
                        Position = Vector3.Zero,
                        Scale = Vector3.One
                    }
                }
            });
        }
    }
}
