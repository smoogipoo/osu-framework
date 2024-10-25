// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneDelayedLoadWrapper : FrameworkTestScene
    {
        private FillFlowContainer<Container> flow = null!;
        private TestSceneDelayedLoadUnloadWrapper.TestScrollContainer scroll = null!;

        private const int panel_count = 2048;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create scroll container", () =>
            {
                Children = new Drawable[]
                {
                    scroll = new TestSceneDelayedLoadUnloadWrapper.TestScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            flow = new FillFlowContainer<Container>
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            }
                        }
                    }
                };
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestManyChildren(bool instant)
        {
            AddStep("create children", () =>
            {
                for (int i = 1; i < panel_count; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadWrapper(new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TestBox { RelativeSizeAxes = Axes.Both }
                                }
                            }, instant ? 0 : 500),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            List<DelayedLoadWrapper> firstLoad = getLoadedWrappers();
            scrollToEnd();
            List<DelayedLoadWrapper> secondLoad = getLoadedWrappers(except: firstLoad);

            AddAssert("more loaded", () => secondLoad.Any());
            AddAssert("not all loaded", () => this.ChildrenOfType<DelayedLoadWrapper>().Any(w => w.Content?.IsLoaded != true));

            AddStep("remove all panels", () => flow.Clear(false));
            AddUntilStep("repeating schedulers removed", () => scroll.Scheduler.HasPendingTasks, () => Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestManyChildrenFunction(bool instant)
        {
            AddStep("create children", () =>
            {
                for (int i = 1; i < panel_count; i++)
                {
                    flow.Add(new Container
                    {
                        Size = new Vector2(128),
                        Children = new Drawable[]
                        {
                            new DelayedLoadWrapper(() => new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TestBox { RelativeSizeAxes = Axes.Both }
                                }
                            }, instant ? 0 : 500),
                            new SpriteText { Text = i.ToString() },
                        }
                    });
                }
            });

            List<DelayedLoadWrapper> firstLoad = getLoadedWrappers();
            scrollToEnd();
            List<DelayedLoadWrapper> secondLoad = getLoadedWrappers(except: firstLoad);

            AddAssert("more loaded", () => secondLoad.Except(firstLoad).Any());
            AddAssert("not all loaded", () => this.ChildrenOfType<DelayedLoadWrapper>().Any(w => w.Content?.IsLoaded != true));

            AddStep("remove all panels", () => flow.Clear(false));
            AddUntilStep("repeating schedulers removed", () => scroll.Scheduler.HasPendingTasks, () => Is.False);
        }

        private List<DelayedLoadWrapper> getLoadedWrappers(List<DelayedLoadWrapper>? except = null)
        {
            except ??= [];

            List<DelayedLoadWrapper> loaded = new List<DelayedLoadWrapper>();

            AddUntilStep("wait for any new loaded", () => this.ChildrenOfType<DelayedLoadWrapper>().Except(except).Any(w => w.Content?.IsLoaded == true));
            AddUntilStep("wait for all pending loads", () => this.ChildrenOfType<DelayedLoadWrapper>().All(d => d.LoadState != LoadState.Loading));
            AddStep("get loaded wrappers", () =>
            {
                loaded.Clear();
                loaded.AddRange(this.ChildrenOfType<DelayedLoadWrapper>().Where(w => w.Content?.IsLoaded == true));
            });

            return loaded;
        }

        private void scrollToEnd()
        {
            AddStep("scroll to end", () => scroll.ScrollToEnd(animated: false));
            AddUntilStep("wait for scroll to complete", () => scroll.IsScrolledToEnd(1));
        }

        public partial class TestBox : Container
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new SpriteText
                {
                    Colour = Color4.Yellow,
                    Text = @"loaded",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            }
        }
    }
}
