// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Input;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneDropdown : ManualInputManagerTestScene
    {
        private const int items_to_add = 10;

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public Task TestBasic()
        {
            TestDropdown[] dropdowns = createDropdowns(2);
            dropdowns[1].AlwaysShowSearchBar = true;
            return Task.CompletedTask;
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestSelectByUserInteraction()
        {
            var testDropdown = createDropdown();

            toggleDropdownViaClick(testDropdown);
            await assertDropdownIsOpen(testDropdown);

            InputManager.MoveMouseTo(testDropdown.Menu.Children[2]);
            InputManager.Click(MouseButton.Left);

            await assertDropdownIsClosed(testDropdown);

            await Assert.That(testDropdown.Current.Value).IsEqualTo(testDropdown.Items.ElementAt(2));
            await Assert.That(testDropdown.SelectedItem.Value?.Identifier).IsEqualTo("test 2");
            await Assert.That((testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                           .SingleOrDefault(i => i.IsSelected)?
                                           .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier)
                        .IsEqualTo("test 2");
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestSelectByCurrent()
        {
            var testDropdown = createDropdown();

            await assertDropdownIsClosed(testDropdown);

            testDropdown.Current.Value = testDropdown.Items.ElementAt(3);

            await Assert.That(testDropdown.Current.Value).IsEqualTo(testDropdown.Items.ElementAt(3));
            await Assert.That(testDropdown.SelectedItem.Value?.Identifier).IsEqualTo("test 3");
            await Assert.That((testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                           .SingleOrDefault(i => i.IsSelected)?
                                           .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier)
                        .IsEqualTo("test 3");
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestClickingDropdownClosesOthers()
        {
            TestDropdown[] dropdowns = createDropdowns(2);

            toggleDropdownViaClick(dropdowns[0], "dropdown 1");
            await assertDropdownIsOpen(dropdowns[0]);

            toggleDropdownViaClick(dropdowns[1], "dropdown 2");
            await assertDropdownIsClosed(dropdowns[0]);
            await assertDropdownIsOpen(dropdowns[1]);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestDropdownHeight()
        {
            const float explicit_height = 100;

            var testDropdown = createDropdown();
            toggleDropdownViaClick(testDropdown);

            for (int i = 0; i < 10; i++)
                testDropdown.AddDropdownItem("test " + (items_to_add + i));
            await Assert.That(testDropdown.Items.Count()).IsEqualTo(items_to_add * 2);

            float calculatedHeight = testDropdown.Menu.Height;
            testDropdown.Menu.MaxHeight = explicit_height;
            await Assert.That(testDropdown.Menu.Height).IsEqualTo(explicit_height);

            testDropdown.Menu.MaxHeight = float.PositiveInfinity;
            await Assert.That(testDropdown.Menu.Height).IsEqualTo(calculatedHeight);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        [Arguments(false)]
        [Arguments(true)]
        public async Task TestKeyboardSelection(bool cleanSelection)
        {
            var testDropdown = createDropdown();

            InputManager.MoveMouseTo(testDropdown.Header);

            if (cleanSelection)
                testDropdown.Current.Value = null;

            int previousIndex = testDropdown.SelectedIndex;
            InputManager.Key(Key.Down);
            await Assert.That(testDropdown.SelectedIndex).IsEqualTo(previousIndex + 1);

            previousIndex = testDropdown.SelectedIndex;
            InputManager.Key(Key.Up);
            await Assert.That(testDropdown.SelectedIndex).IsEqualTo(Math.Max(0, previousIndex - 1));

            InputManager.Keys(PlatformAction.MoveToListEnd);
            await Assert.That<MenuItem>(testDropdown.SelectedItem).IsEqualTo(testDropdown.Menu.VisibleMenuItems.Last().Item);

            InputManager.Keys(PlatformAction.MoveToListStart);
            await Assert.That<MenuItem>(testDropdown.SelectedItem).IsEqualTo(testDropdown.Menu.VisibleMenuItems.First().Item);

            InputManager.Key(Key.Up);
            InputManager.Key(Key.Down);
            InputManager.Key(Key.PageUp);
            InputManager.Key(Key.PageDown);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestReplaceItems()
        {
            var testDropdown = createDropdown();

            toggleDropdownViaClick(testDropdown);

            InputManager.MoveMouseTo(testDropdown.Menu.Children[4]);
            InputManager.Click(MouseButton.Left);
            await Assert.That(testDropdown.Current.Value?.Identifier).IsEqualTo("test 4");

            testDropdown.Items = testDropdown.Items.Select(i => new TestModel(i.AsNonNull().ToString())).ToArray();

            await Assert.That(testDropdown.Current.Value?.Identifier).IsEqualTo("test 4");
            await Assert.That(testDropdown.SelectedItem.Value?.Identifier).IsEqualTo("test 4");
            await Assert.That((testDropdown.ChildrenOfType<Dropdown<TestModel?>.DropdownMenu.DrawableDropdownMenuItem>()
                                           .SingleOrDefault(i => i.IsSelected)?
                                           .Item as DropdownMenuItem<TestModel?>)?.Value?.Identifier)
                        .IsEqualTo("test 4");
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestInvalidCurrent()
        {
            var testDropdown = createDropdown();

            toggleDropdownViaClick(testDropdown);
            testDropdown.Current.Value = "invalid";

            await Assert.That(testDropdown.Current.Value?.Identifier).IsEqualTo("invalid");
            await Assert.That(testDropdown.Header.Label.ToString()).IsEqualTo("invalid");

            testDropdown.Current.Value = testDropdown.Items.ElementAt(2);
            await Assert.That(testDropdown.Current.Value).IsEqualTo(testDropdown.Items.ElementAt(2));
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestNullCurrent()
        {
            var testDropdown = createDropdown();

            testDropdown.Current.Value = testDropdown.Items.ElementAt(1);
            await Assert.That(testDropdown.Current.Value).IsEqualTo(testDropdown.Items.ElementAt(1));

            testDropdown.Current.Value = null;
            await Assert.That(testDropdown.Current.Value).IsEqualTo(null);
            await Assert.That(testDropdown.Header.Label.ToString()).IsNullOrEmpty();
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestDisabledCurrent()
        {
            var testDropdown = createDropdown();
            testDropdown.Current.Disabled = true;

            var originalValue = testDropdown.Current.Value.AsNonNull();

            toggleDropdownViaClick(testDropdown);
            await assertDropdownIsClosed(testDropdown);

            InputManager.Key(Key.Down);
            await valueIsUnchanged();

            InputManager.Key(Key.Up);
            await valueIsUnchanged();

            InputManager.Keys(PlatformAction.MoveToListStart);
            await valueIsUnchanged();

            InputManager.Keys(PlatformAction.MoveToListEnd);
            await valueIsUnchanged();

            testDropdown.Current.Disabled = false;
            toggleDropdownViaClick(testDropdown);
            await assertDropdownIsOpen(testDropdown);

            testDropdown.Current.Disabled = true;
            await assertDropdownIsClosed(testDropdown);

            async Task valueIsUnchanged() => await Assert.That(testDropdown.Current.Value).IsEqualTo(originalValue);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestItemSource()
        {
            var testDropdown = createDropdown();

            BindableList<TestModel?> bindableList;
            testDropdown.ItemSource = bindableList = new BindableList<TestModel?>();

            toggleDropdownViaClick(testDropdown);
            await Assert.That(testDropdown.Items).IsEmpty();

            bindableList.AddRange(new[] { "one", "2", "three" }.Select(s => new TestModel(s)));
            testDropdown.Current.Value = "three";

            bindableList.ReplaceRange(1, 1, new TestModel[] { "two" });
            await checkOrder(1, "two");

            bindableList.RemoveAt(0);
            await Assert.That(testDropdown.Items).HasCount().EqualTo(2);
            await Assert.That(testDropdown.Current.Value?.Identifier).IsEqualTo("three");

            bindableList.Remove("three");
            await Assert.That(testDropdown.Current.Value?.Identifier).IsEqualTo("two");

            bindableList.Insert(0, "one");
            bindableList.Add("three");

            await checkOrder(0, "one");
            await checkOrder(2, "three");

            bindableList.Add("one-half");
            bindableList.Move(3, 1);
            await checkOrder(1, "one-half");
            await checkOrder(2, "two");

            async Task checkOrder(int index, string item) => await Assert.That(testDropdown.ChildrenOfType<FillFlowContainer<Menu.DrawableMenuItem>>().Single()
                                                                                           .FlowingChildren.Cast<Menu.DrawableMenuItem>().ElementAt(index).Item.Text.Value.ToString())
                                                                         .IsEqualTo(item);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestExternalManagement()
        {
            var dropdown = createDropdown();
            dropdown.AlwaysShowSearchBar = true;

            Drawable openButton;
            Add(openButton = new BasicButton
            {
                Size = new Vector2(150, 30),
                Position = new Vector2(225, 50),
                Text = "Open dropdown",
                Action = openExternally
            });

            // Open via setting state directly

            openExternally();
            await assertDropdownIsOpen(dropdown);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);
            InputManager.Key(Key.Escape);

            // Open via clicking on an external button

            InputManager.MoveMouseTo(openButton);
            InputManager.Click(MouseButton.Left);

            await assertDropdownIsOpen(dropdown);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);
            InputManager.Key(Key.Escape);

            // Close via setting state directly

            openExternally();
            await assertDropdownIsOpen(dropdown);
            dropdown.ChildrenOfType<Menu>().Single().Close();
            await assertDropdownIsClosed(dropdown);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);

            void openExternally() => dropdown.ChildrenOfType<Menu>().Single().Open();
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestItemReplacementDoesNotAffectScroll()
        {
            var testDropdown = createDropdown();

            BindableList<TestModel?> bindableList;
            testDropdown.ItemSource = bindableList = new BindableList<TestModel?>();
            bindableList.AddRange(Enumerable.Range(0, 20).Select(i => (TestModel)$"test {i}"));
            testDropdown.Menu.MaxHeight = 100;

            toggleDropdownViaClick(testDropdown);

            testDropdown.ChildrenOfType<BasicScrollContainer>().Single().ScrollTo(200);
            bindableList.ReplaceRange(10, 1, new TestModel[] { "test ten" });
            await Assert.That(testDropdown.ChildrenOfType<BasicScrollContainer>().Single().Target).IsEqualTo(200);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestClearItemsInBindableWhileNotPresent()
        {
            var testDropdown = createDropdown();

            BindableList<TestModel?> bindableList;
            testDropdown.ItemSource = bindableList = new BindableList<TestModel?>();
            bindableList.AddRange(Enumerable.Range(0, 20).Select(i => (TestModel)$"test {i}"));

            testDropdown.Hide();
            bindableList.Clear();
            testDropdown.Show();
            await Assert.That(testDropdown.Menu.Children).IsEmpty();
        }

        /// <summary>
        /// Adds an item before a dropdown is loaded, and ensures item labels are assigned correctly.
        /// </summary>
        /// <remarks>
        /// Ensures item labels are assigned after the dropdown finishes loading (reaches <see cref="LoadState.Ready"/> state),
        /// so any dependency from BDL can be retrieved first before calling <see cref="Dropdown{T}.GenerateItemText"/>.
        /// </remarks>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestAddItemBeforeDropdownLoad()
        {
            BdlDropdown dropdown;
            Child = dropdown = new BdlDropdown
            {
                Position = new Vector2(50f, 50f),
                Width = 150f,
                Items = new TestModel("test").Yield(),
            };

            await Assert.That(dropdown.Menu.VisibleMenuItems.First().ChildrenOfType<SpriteText>().First().Text.ToString())
                        .IsEqualTo("loaded: test");
        }

        /// <summary>
        /// Adds an item after the dropdown is in <see cref="LoadState.Ready"/> state, and ensures item labels are assigned correctly and not ignored by <see cref="Dropdown{T}"/>.
        /// </summary>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestAddItemWhileDropdownIsInReadyState()
        {
            BdlDropdown dropdown;
            Child = dropdown = new BdlDropdown
            {
                Position = new Vector2(50f, 50f),
                Width = 150f,
            };

            dropdown.Items = new TestModel("test").Yield();

            await Assert.That(dropdown.Menu.VisibleMenuItems.First(d => d.IsSelected).ChildrenOfType<SpriteText>().First().Text.ToString())
                        .IsEqualTo("loaded: test");
        }

        /// <summary>
        /// Sets a non-existent item dropdown and ensures its label is assigned correctly.
        /// </summary>
        /// <param name="afterBdl">Whether the non-existent item should be set before or after the dropdown's BDL has run.</param>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        [Arguments(true)]
        [Arguments(false)]
        public async Task TestSetNonExistentItem(bool afterBdl)
        {
            BindableList<TestModel?> bindableList = new BindableList<TestModel?>();
            bindableList.AddRange(new[] { "one", "two", "three" }.Select(s => new TestModel(s)));

            var bindable = new Bindable<TestModel?>();

            if (!afterBdl)
                bindable.Value = new TestModel("non-existent item");

            BdlDropdown dropdown;
            Child = dropdown = new BdlDropdown
            {
                Position = new Vector2(50f, 50f),
                Width = 150f,
                ItemSource = bindableList,
                Current = bindable,
            };

            if (afterBdl)
                bindable.Value = new TestModel("non-existent item");

            await Assert.That(dropdown.SelectedItem.Text.Value.ToString()).IsEqualTo("loaded: non-existent item");
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestRemoveDropdownOnSelect()
        {
            Bindable<TestModel?> bindable = new Bindable<TestModel?>();
            bindable.ValueChanged += _ => createDropdown();

            var testDropdown = createDropdown();
            testDropdown.Current = bindable;

            toggleDropdownViaClick(testDropdown);

            InputManager.MoveMouseTo(testDropdown.Menu.Children[2]);
            InputManager.Click(MouseButton.Left);

            await Assert.That(bindable.Value?.Identifier).IsEqualTo("test 2");
        }

        #region Searching

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestSearching()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);

            toggleDropdownViaClick(dropdown);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);

            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);
            await Assert.That(dropdown.Menu.VisibleMenuItems.Single(i => i.IsPresent).Item.Text.Value).IsEqualTo("test 4");
            await Assert.That(dropdown.Menu.VisibleMenuItems.Single().IsPreSelected).IsTrue();

            InputManager.Key(Key.Enter);
            await Assert.That(dropdown.SelectedItem.Text.Value).IsEqualTo("test 4");
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestReleaseFocusAfterSearching()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            toggleDropdownViaClick(dropdown);

            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            InputManager.Key(Key.Escape);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            await assertDropdownIsOpen(dropdown);

            InputManager.Key(Key.Escape);
            await assertDropdownIsClosed(dropdown);

            toggleDropdownViaClick(dropdown);
            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            InputManager.MoveMouseTo(Vector2.Zero);
            InputManager.Click(MouseButton.Left);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestSelectSearchedItem()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            toggleDropdownViaClick(dropdown);

            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            InputManager.Key(Key.Enter);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            await assertDropdownIsClosed(dropdown);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestAlwaysShowSearchBar()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            dropdown.AlwaysShowSearchBar = true;

            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            toggleDropdownViaClick(dropdown);

            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            InputManager.Key(Key.Escape);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);
            await assertDropdownIsOpen(dropdown);

            InputManager.Key(Key.Escape);
            await assertDropdownIsClosed(dropdown);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);

            toggleDropdownViaClick(dropdown);
            dropdown.TextInput.Text("test 4");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);

            InputManager.MoveMouseTo(Vector2.Zero);
            InputManager.Click(MouseButton.Left);
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestKeyBindingIsolation()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            dropdown.AlwaysShowSearchBar = true;

            TestKeyBindingHandler keyBindingHandler;
            Add(new TestKeyBindingContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = keyBindingHandler = new TestKeyBindingHandler
                {
                    RelativeSizeAxes = Axes.Both,
                },
            });

            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            toggleDropdownViaClick(dropdown);

            InputManager.Key(Key.Space);
            // we must send something via the text input path for TextBox to block the space key press above,
            // we're not supposed to do this here, but we don't have a good way of simulating text input from ManualInputManager so let's just do this for now.
            // todo: add support for simulating text typing at a ManualInputManager level for more realistic results.
            dropdown.TextInput.Text(" ");

            await Assert.That(keyBindingHandler.ReceivedPress).IsFalse();

            toggleDropdownViaClick(dropdown);

            InputManager.Key(Key.Space);
            // we must send something via the text input path for TextBox to block the space key press above,
            // we're not supposed to do this here, but we don't have a good way of simulating text input from ManualInputManager so let's just do this for now.
            dropdown.TextInput.Text(" ");

            await Assert.That(keyBindingHandler.ReceivedPress).IsTrue();
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestMouseFromTouch()
        {
            var dropdown = createDropdowns<ManualTextDropdown>(1)[0];
            dropdown.AlwaysShowSearchBar = true;

            TestClickHandler clickHandler;
            Add(clickHandler = new TestClickHandler
            {
                RelativeSizeAxes = Axes.Both
            });

            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            InputManager.BeginTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre));
            InputManager.EndTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre));

            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            await Assert.That(clickHandler.ReceivedClick).IsTrue();

            dropdown.TextInput.Text("something");
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Hidden);
            await Assert.That(dropdown.Header.SearchTerm.Value).IsNullOrEmpty();

            clickHandler.Hide();
            InputManager.BeginTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre));
            InputManager.EndTouch(new Touch(TouchSource.Touch1, dropdown.Header.ScreenSpaceDrawQuad.Centre));
            await Assert.That(dropdown.ChildrenOfType<DropdownSearchBar>().Single().State.Value).IsEqualTo(Visibility.Visible);
        }

        #endregion

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestPaddedSearchBar()
        {
            SearchBarPaddedDropdown dropdown;
            Child = dropdown = new SearchBarPaddedDropdown
            {
                Position = new Vector2(50f, 50f),
                Width = 150f,
                Items = new TestModel("test").Yield(),
            };

            RectangleF area = dropdown.Header.ScreenSpaceDrawQuad.AABBFloat;
            InputManager.MoveMouseTo(new Vector2(area.Right - 5, area.Centre.Y));
            InputManager.Click(MouseButton.Left);

            await assertDropdownIsOpen(dropdown);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        [Arguments(false)]
        [Arguments(true)]
        public async Task TestDoubleClickOnHeader(bool alwaysShowSearchBar)
        {
            bool wasOpened = false;
            bool wasClosed = false;

            var testDropdown = createDropdown();
            testDropdown.AlwaysShowSearchBar = alwaysShowSearchBar;
            testDropdown.Menu.StateChanged += s =>
            {
                wasOpened |= s == MenuState.Open;
                wasClosed |= s == MenuState.Closed;
            };

            InputManager.MoveMouseTo(testDropdown.Header);
            InputManager.Click(MouseButton.Left);
            InputManager.Click(MouseButton.Left);

            await Assert.That(wasOpened).IsTrue();
            await Assert.That(wasClosed).IsTrue();
            await assertDropdownIsClosed(testDropdown);
        }

        private TestDropdown createDropdown() => createDropdowns(1).Single();

        private TestDropdown[] createDropdowns(int count) => createDropdowns<TestDropdown>(count);

        private TDropdown[] createDropdowns<TDropdown>(int count)
            where TDropdown : TestDropdown, new()
        {
            TDropdown[] dropdowns = new TDropdown[count];

            for (int dropdownIndex = 0; dropdownIndex < count; dropdownIndex++)
            {
                var testItems = new TestModel[10];
                for (int itemIndex = 0; itemIndex < items_to_add; itemIndex++)
                    testItems[itemIndex] = "test " + itemIndex;

                dropdowns[dropdownIndex] = new TDropdown
                {
                    Position = new Vector2(50f, 50f),
                    Width = 150,
                    Items = testItems,
                };
            }

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(50),
                Spacing = new Vector2(20f, 20f),
                Direction = FillDirection.Horizontal,
                Children = dropdowns,
            };

            return dropdowns;
        }

        private void toggleDropdownViaClick(TestDropdown dropdown, string? dropdownName = null)
        {
            InputManager.MoveMouseTo(dropdown.Header);
            InputManager.Click(MouseButton.Left);
        }

        private async Task assertDropdownIsOpen(TestDropdown dropdown)
            => await Assert.That(dropdown.Menu.State).IsEqualTo(MenuState.Open);

        private async Task assertDropdownIsClosed(TestDropdown dropdown)
            => await Assert.That(dropdown.Menu.State).IsEqualTo(MenuState.Closed);

        private class TestModel : IEquatable<TestModel>
        {
            public readonly string Identifier;

            public TestModel(string identifier)
            {
                Identifier = identifier;
            }

            public bool Equals(TestModel? other)
            {
                if (other == null)
                    return false;

                return other.Identifier == Identifier;
            }

            public override int GetHashCode() => Identifier.GetHashCode();

            public override string ToString() => Identifier;

            public static implicit operator TestModel(string str) => new TestModel(str);
        }

        private partial class TestDropdown : BasicDropdown<TestModel?>
        {
            internal new DropdownMenuItem<TestModel?> SelectedItem => base.SelectedItem;

            public int SelectedIndex => Menu.VisibleMenuItems.Select(d => d.Item).ToList().IndexOf(SelectedItem);
            public int PreselectedIndex => Menu.VisibleMenuItems.ToList().IndexOf(Menu.PreselectedItem);
        }

        private partial class ManualTextDropdown : TestDropdown
        {
            [Cached(typeof(TextInputSource))]
            public readonly ManualTextInputSource TextInput = new ManualTextInputSource();
        }

        /// <summary>
        /// Dropdown that will access state set by BDL load in <see cref="GenerateItemText"/>.
        /// </summary>
        private partial class BdlDropdown : TestDropdown
        {
            private string text = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                text = "loaded";
            }

            protected override LocalisableString GenerateItemText(TestModel? item)
            {
                Trace.Assert(text != null);
                return $"{text}: {base.GenerateItemText(item)}";
            }
        }

        private partial class SearchBarPaddedDropdown : TestDropdown
        {
            protected override DropdownHeader CreateHeader() => new PaddedHeader();

            private partial class PaddedHeader : BasicDropdownHeader
            {
                protected override DropdownSearchBar CreateSearchBar() => base.CreateSearchBar().With(d =>
                {
                    d.Padding = new MarginPadding { Right = 25 };
                });
            }
        }

        private partial class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.Space, TestAction.SpaceAction)
            };
        }

        private partial class TestKeyBindingHandler : Drawable, IKeyBindingHandler<TestAction>
        {
            public bool ReceivedPress;

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                ReceivedPress = true;
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
            }
        }

        private partial class TestClickHandler : Drawable
        {
            public bool ReceivedClick;

            protected override bool OnClick(ClickEvent e)
            {
                ReceivedClick = true;
                return true;
            }
        }

        private enum TestAction
        {
            SpaceAction,
        }
    }
}
