// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;
using TUnit.Assertions.Extensions.Numbers;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    [HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
    public partial class TestSceneButton : ManualInputManagerTestScene
    {
        private int clickCount;
        private readonly BasicButton button;

        public TestSceneButton()
        {
            Add(button = new BasicButton
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Text = "this is a button",
                Size = new Vector2(200, 40),
                Margin = new MarginPadding(10),
                FlashColour = FrameworkColour.Green,
                Action = () => clickCount++
            });
        }

        [Before(Test), HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public new Task SetUp()
        {
            clickCount = 0;
            button.Enabled.Value = true;
            return Task.CompletedTask;
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task Button()
        {
            InputManager.MoveMouseTo(button.ScreenSpaceDrawQuad.Centre);
            InputManager.Click(MouseButton.Left);

            await Assert.That(() => clickCount).IsEqualTo(1).AtSomePoint();
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task DisabledButton()
        {
            button.Enabled.Value = false;

            InputManager.MoveMouseTo(button.ScreenSpaceDrawQuad.Centre);
            InputManager.Click(MouseButton.Left);

            await Assert.That(() => clickCount).IsZero();
        }
    }
}
