// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK.Input;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneClosableMenu : MenuTestScene
    {
        [SetUpSteps]
        public void SetUpSteps()
        {
            CreateMenu(() => new AnimatedMenu(Direction.Vertical)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                State = MenuState.Open,
                Items = new[]
                {
                    new MenuItem("Item #1")
                    {
                        Items = new[]
                        {
                            new MenuItem("Sub-item #1"),
                            new MenuItem("Sub-item #2", () => { }),
                        }
                    },
                    new MenuItem("Item #2")
                    {
                        Items = new[]
                        {
                            new MenuItem("Sub-item #1"),
                            new MenuItem("Sub-item #2", () => { }),
                        }
                    },
                }
            });
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestClickItemWithoutActionDoesNotCloseMenus()
        {
            ClickItem(0, 0);
            ClickItem(1, 0);

            await using (Assert.Multiple())
            {
                for (int i = 1; i >= 0; --i)
                    Assert.That(Menus.GetSubMenu(i).State).IsEqualTo(MenuState.Open);
            }
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestClickItemWithActionAssignedDuringNavigationClosesMenus()
        {
            ClickItem(0, 0);
            InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[0]);
            Menus.GetSubStructure(1).GetMenuItems().Cast<Menu.DrawableMenuItem>().First().Item.Action.Value = () => { };
            InputManager.Click(MouseButton.Left);

            await using (Assert.Multiple())
            {
                for (int i = 1; i >= 0; --i)
                    Assert.That(Menus.GetSubMenu(i).State).IsEqualTo(MenuState.Closed);
            }
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestClickItemWithActionClosesMenus()
        {
            ClickItem(0, 0);
            ClickItem(1, 1);

            await using (Assert.Multiple())
            {
                for (int i = 1; i >= 0; --i)
                    Assert.That(Menus.GetSubMenu(i).State).IsEqualTo(MenuState.Closed);
            }
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuIgnoresEscapeWhenClosed()
        {
            var menu = (AnimatedMenu)Menus.GetSubMenu(0);
            InputManager.Key(Key.Escape);

            await Assert.That(menu.PressBlocked).IsTrue();

            menu.PressBlocked = false;
            InputManager.Key(Key.Escape);

            await Assert.That(menu.PressBlocked).IsFalse();
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMenuBlocksInputUnderneathIt()
        {
            bool itemClicked = false;
            bool actionReceived = false;

            Menus.GetSubMenu(0).Items[0].Items[0].Action.Value = () => itemClicked = true;

            Add(new MouseHandlingLayer
            {
                Action = () => actionReceived = true,
                Depth = 1,
            });

            ClickItem(0, 0);
            ClickItem(1, 0);

            await Assert.That(itemClicked).IsTrue();
            await Assert.That(actionReceived).IsFalse();
        }

        private partial class MouseHandlingLayer : Drawable
        {
            public Action? Action { get; set; }

            public MouseHandlingLayer()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                Action?.Invoke();
                return base.OnMouseDown(e);
            }
        }

        private partial class AnimatedMenu : BasicMenu
        {
            public bool PressBlocked { get; set; }

            public AnimatedMenu(Direction direction)
                : base(direction)
            {
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                return PressBlocked = base.OnKeyDown(e);
            }

            protected override void AnimateOpen() => this.FadeIn(500);

            protected override void AnimateClose() => this.FadeOut(5000); // Ensure escape is pressed while menu is still fading

            protected override Menu CreateSubMenu() => new AnimatedMenu(Direction);
        }
    }
}
