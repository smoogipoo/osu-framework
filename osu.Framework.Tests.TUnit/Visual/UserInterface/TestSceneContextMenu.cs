// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneContextMenu : ManualInputManagerTestScene
    {
        protected override Container<Drawable> Content => contextMenuContainer ?? base.Content;

        private readonly TestContextMenuContainer contextMenuContainer;

        public TestSceneContextMenu()
        {
            base.Content.Add(contextMenuContainer = new TestContextMenuContainer { RelativeSizeAxes = Axes.Both });
        }

        [Before(Test), HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public Task Setup()
        {
            Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tests an edge case where the submenu is visible and continues updating for a short period of time after right clicking another item.
        /// In such a case, the submenu should not update its position unless it's open.
        /// </summary>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestNestedMenuTransferredWithFadeOut()
        {
            TestContextMenuContainerWithFade fadingMenuContainer;
            BoxWithNestedContextMenuItems box1;
            BoxWithNestedContextMenuItems box2;

            Child = fadingMenuContainer = new TestContextMenuContainerWithFade
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10),
                    Children = new[]
                    {
                        box1 = new BoxWithNestedContextMenuItems { Size = new Vector2(100) },
                        box2 = new BoxWithNestedContextMenuItems { Size = new Vector2(100) }
                    }
                }
            };

            clickBoxStep(box1);
            InputManager.MoveMouseTo(fadingMenuContainer.ChildrenOfType<Menu.DrawableMenuItem>().First());

            clickBoxStep(box2);
            InputManager.MoveMouseTo(fadingMenuContainer.ChildrenOfType<Menu.DrawableMenuItem>().First());

            var targetItem = fadingMenuContainer.ChildrenOfType<Menu.DrawableMenuItem>().First();
            var subMenu = fadingMenuContainer.ChildrenOfType<Menu>().Last();

            await Assert.That(subMenu.State).IsEqualTo(MenuState.Open);
            await Assert.That(subMenu.IsPresent).IsTrue();
            await Assert.That(subMenu.IsMaskedAway).IsFalse();
            await Assert.That(subMenu.ScreenSpaceDrawQuad.TopLeft.X).IsGreaterThan(targetItem.ScreenSpaceDrawQuad.TopLeft.X);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuOpenedOnClick()
        {
            Drawable box = addBoxStep(1);

            clickBoxStep(box);

            await assertMenuState(true);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuClosedOnClickOutside()
        {
            var box = addBoxStep(1);

            clickBoxStep(box);
            clickOutsideStep();

            await assertMenuState(false);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuTransferredToNewTarget()
        {
            var box1 = addBoxStep(1).With(d =>
            {
                d.X = -100;
                d.Colour = Color4.Green;
            });

            var box2 = addBoxStep(1).With(d =>
            {
                d.X = -100;
                d.Colour = Color4.Red;
            });

            clickBoxStep(box1);
            clickBoxStep(box2);

            await assertMenuState(true);
            await assertMenuInCentre(box2);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuHiddenWhenTargetHidden()
        {
            Drawable box = addBoxStep(1);

            clickBoxStep(box);
            box.Hide();

            await assertMenuState(false);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuTracksMovement()
        {
            Drawable box = addBoxStep(1);

            clickBoxStep(box);
            box.X += 100;

            await assertMenuInCentre(box);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        [Arguments(Anchor.TopLeft)]
        [Arguments(Anchor.TopCentre)]
        [Arguments(Anchor.TopRight)]
        [Arguments(Anchor.CentreLeft)]
        [Arguments(Anchor.CentreRight)]
        [Arguments(Anchor.BottomLeft)]
        [Arguments(Anchor.BottomCentre)]
        [Arguments(Anchor.BottomRight)]
        public async Task TestMenuOnScreenWhenTargetPartlyOffScreen(Anchor anchor)
        {
            Drawable box = addBoxStep(5);

            clickBoxStep(box);
            box.Anchor = anchor;
            box.X -= 5;
            box.Y -= 5;

            await assertMenuOnScreen(true);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        [Arguments(Anchor.TopLeft)]
        [Arguments(Anchor.TopCentre)]
        [Arguments(Anchor.TopRight)]
        [Arguments(Anchor.CentreLeft)]
        [Arguments(Anchor.CentreRight)]
        [Arguments(Anchor.BottomLeft)]
        [Arguments(Anchor.BottomCentre)]
        [Arguments(Anchor.BottomRight)]
        public async Task TestMenuNotOnScreenWhenTargetSignificantlyOffScreen(Anchor anchor)
        {
            Drawable box = addBoxStep(5);

            clickBoxStep(box);
            box.Anchor = anchor;

            if (anchor.HasFlagFast(Anchor.x0))
                box.X -= contextMenuContainer.CurrentMenu.DrawWidth + 10;
            else if (anchor.HasFlagFast(Anchor.x2))
                box.X += 10;

            if (anchor.HasFlagFast(Anchor.y0))
                box.Y -= contextMenuContainer.CurrentMenu.DrawHeight + 10;
            else if (anchor.HasFlagFast(Anchor.y2))
                box.Y += 10;

            await assertMenuOnScreen(false);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestReturnNullInNestedDrawableOpensParentMenu()
        {
            addBoxStep(2);
            Drawable box2 = addBoxStep();

            clickBoxStep(box2);

            await assertMenuState(true);
            await assertMenuItems(2);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestReturnEmptyInNestedDrawableBlocksMenuOpening()
        {
            addBoxStep(2);
            Drawable box2 = addBoxStep();

            clickBoxStep(box2);

            await assertMenuState(false);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestHideWhileScrolledAndShow()
        {
            var box = addBoxStep(1);

            clickBoxStep(box);
            await assertMenuState(true);

            InputManager.MoveMouseTo(contextMenuContainer.CurrentMenu);
            InputManager.PressButton(MouseButton.Left);
            InputManager.MoveMouseTo(contextMenuContainer.CurrentMenu, new Vector2(0, 150));

            InputManager.Key(Key.Escape);
            InputManager.ReleaseButton(MouseButton.Left);

            clickBoxStep(box);
            await Assert.That(contextMenuContainer.CurrentMenu.DrawSize.Y).IsGreaterThan(10);
        }

        private void clickBoxStep(Drawable box)
        {
            InputManager.MoveMouseTo(box);
            InputManager.Click(MouseButton.Right);
        }

        private void clickOutsideStep()
        {
            InputManager.MoveMouseTo(InputManager.ScreenSpaceDrawQuad.TopLeft);
            InputManager.Click(MouseButton.Right);
        }

        private Drawable addBoxStep(int actionCount) => addBoxStep(Enumerable.Repeat(() => { }, actionCount).ToArray());

        private Drawable addBoxStep(params Action[] actions)
        {
            var box = new BoxWithContextMenu(actions)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
            };

            Add(box);

            return box;
        }

        private async Task assertMenuState(bool opened)
            => await Assert.That(contextMenuContainer.CurrentMenu.State).IsEqualTo(opened ? MenuState.Open : MenuState.Closed);

        private async Task assertMenuInCentre(Drawable box)
            => await Assert.That(Precision.AlmostEquals(contextMenuContainer.CurrentMenu.ScreenSpaceDrawQuad.TopLeft, box.ScreenSpaceDrawQuad.Centre)).IsTrue();

        private async Task assertMenuOnScreen(bool expected)
        {
            var inputQuad = InputManager.ScreenSpaceDrawQuad;
            var menuQuad = contextMenuContainer.CurrentMenu.ScreenSpaceDrawQuad;

            bool result = inputQuad.Contains(menuQuad.TopLeft + new Vector2(1, 1))
                          && inputQuad.Contains(menuQuad.TopRight + new Vector2(-1, 1))
                          && inputQuad.Contains(menuQuad.BottomLeft + new Vector2(1, -1))
                          && inputQuad.Contains(menuQuad.BottomRight + new Vector2(-1, -1));

            await Assert.That(result).IsEqualTo(expected);
        }

        private async Task assertMenuItems(int expectedCount)
            => await Assert.That(contextMenuContainer.CurrentMenu.Items.Count).IsEqualTo(expectedCount);

        private partial class BoxWithContextMenu : Box, IHasContextMenu
        {
            private readonly Action[] actions;

            public BoxWithContextMenu(Action[] actions)
            {
                this.actions = actions;
            }

            public MenuItem[] ContextMenuItems => actions.Select((a, i) => new MenuItem($"Item {i}", a)).ToArray();
        }

        private partial class BoxWithNestedContextMenuItems : Box, IHasContextMenu
        {
            public MenuItem[] ContextMenuItems => new[]
            {
                new MenuItem("First")
                {
                    Items = new[]
                    {
                        new MenuItem("Second")
                    }
                },
            };
        }

        private partial class TestContextMenuContainer : BasicContextMenuContainer
        {
            public Menu CurrentMenu { get; private set; } = null!;

            protected override Menu CreateMenu() => CurrentMenu = base.CreateMenu();
        }

        private partial class TestContextMenuContainerWithFade : BasicContextMenuContainer
        {
            protected override Menu CreateMenu() => new TestMenu();

            private partial class TestMenu : BasicMenu
            {
                public TestMenu()
                    : base(Direction.Vertical)
                {
                    ItemsContainer.Padding = new MarginPadding { Vertical = 2 };
                }

                protected override void AnimateClose() => this.FadeOut(1000, Easing.OutQuint);

                protected override Menu CreateSubMenu() => new TestMenu();
            }
        }
    }
}
