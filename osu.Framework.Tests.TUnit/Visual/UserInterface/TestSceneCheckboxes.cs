// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneCheckboxes : FrameworkTestScene
    {
        private readonly BasicCheckbox basic;

        public TestSceneCheckboxes()
        {
            BasicCheckbox swap, rotate;

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Padding = new MarginPadding(10),
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        basic = new BasicCheckbox
                        {
                            LabelText = @"Basic Test"
                        },
                        new BasicCheckbox
                        {
                            LabelText = @"FadeDuration Test",
                            FadeDuration = 300
                        },
                        swap = new BasicCheckbox
                        {
                            LabelText = @"Checkbox Position",
                        },
                        rotate = new BasicCheckbox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };

            swap.Current.ValueChanged += check => swap.RightHandedCheckbox = check.NewValue;
            rotate.Current.ValueChanged += e => rotate.RotateTo(e.NewValue ? 45 : 0, 100);
        }

        /// <summary>
        /// Test safety of <see cref="IHasCurrentValue{T}"/> implementation.
        /// This is shared across all UI elements.
        /// </summary>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestDirectToggle()
        {
            var testBindable = new Bindable<bool> { BindTarget = basic.Current };

            await Assert.That(basic.Current.Value).IsFalse();
            await Assert.That(testBindable.Value).IsFalse();

            basic.Current.Value = true;

            await Assert.That(basic.Current.Value).IsTrue();
            await Assert.That(testBindable.Value).IsTrue();

            basic.Current = new Bindable<bool>();

            await Assert.That(basic.Current.Value).IsFalse();
            await Assert.That(testBindable.Value).IsFalse();
        }
    }
}
