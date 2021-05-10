// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestSceneLayoutIssue : FrameworkTestScene
    {
        [Test]
        public void TestCase1()
        {
            Drawable nested = null;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Green
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Child = nested = new Box
                            {
                                Size = new Vector2(5),
                                Colour = Color4.Red,
                                Alpha = 0.5f
                            }
                        }
                    }
                };
            });

            AddStep("update nested size", () =>
            {
                ScheduleAfterChildren(() =>
                {
                    nested.Size = new Vector2(100, 50);
                });
            });
        }

        [Test]
        public void TestCase1Text()
        {
            TestContainer t = null;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.SlateGray
                        },
                        t = new TestContainer()
                    }
                };
            });

            AddStep("set some text", () => t.Text = "whoops!");
        }

        [Test]
        public void TestCase2()
        {
            TestContainer t = null;

            AddStep("create test", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200), // Assume potentially given by other children.
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.SlateGray
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = t = new TestContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            }
                        }
                    }
                };
            });

            AddStep("set some text", () => t.Text = "whoops!");
        }

        private class TestContainer : CompositeDrawable
        {
            public LocalisableString Text
            {
                get => text.Text;
                set => text.Text = value;
            }

            private readonly SpriteText text;

            public TestContainer()
            {
                AutoSizeAxes = Axes.Both;

                InternalChild = text = new SpriteText();
            }
        }
    }
}
