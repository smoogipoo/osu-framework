// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using TUnit.Core.Executors;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneColourPicker : FrameworkTestScene
    {
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestExternalColourSetAfterCreation()
        {
            ColourPicker colourPicker;
            Child = colourPicker = new BasicColourPicker();

            colourPicker.Current.Value = Colour4.Goldenrod;
            await Assert.That(colourPicker.Current.Value).IsEqualTo(Colour4.Goldenrod);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestExternalColourSetAtCreation()
        {
            ColourPicker colourPicker;
            Child = colourPicker = new BasicColourPicker
            {
                Current = { Value = Colour4.Goldenrod }
            };

            await Assert.That(colourPicker.Current.Value).IsEqualTo(Colour4.Goldenrod);
        }

        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public async Task TestExternalHSVChange()
        {
            const float hue = 0.34f;
            const float saturation = 0.46f;
            const float value = 0.84f;

            ColourPicker colourPicker;

            Child = colourPicker = new BasicColourPicker
            {
                Current = { Value = Colour4.Goldenrod }
            };

            colourPicker.Hide();

            var saturationValueControl = this.ChildrenOfType<HSVColourPicker.SaturationValueSelector>().Single();

            saturationValueControl.Hue.Value = hue;
            saturationValueControl.Saturation.Value = saturation;
            saturationValueControl.Value.Value = value;

            await Assert.That(colourPicker.Current.Value).IsEqualTo(Colour4.FromHSV(hue, saturation, value));
        }
    }
}
