// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestScenePerspectiveContainer : FrameworkTestScene
    {
        public TestScenePerspectiveContainer()
        {
            PerspectiveContainer c;

            Child = c = new PerspectiveContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.5f)
                }
            };

            AddSliderStep("A", -2f, 2f, 0f, v => c.A = v / 100);
            AddSliderStep("B", -2f, 2f, 0f, v => c.B = v / 100);
        }

        private class PerspectiveContainer : Container
        {
            private float a = 0.004f;

            public float A
            {
                get => a;
                set
                {
                    if (a == value)
                        return;

                    a = value;
                    Invalidate(Invalidation.DrawInfo);
                }
            }

            private float b = 0;

            public float B
            {
                get => b;
                set
                {
                    if (b == value)
                        return;

                    b = value;
                    Invalidate(Invalidation.DrawInfo);
                }
            }

            protected override DrawInfo ComputeDrawInfo()
            {
                var d = base.ComputeDrawInfo();
                d.ApplyPerspective(new Vector2(a, b));
                return d;
            }
        }
    }
}
