// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; } = null!;

        private Bindable<Size> configWindowSize = null!;

        private SpriteText infoText = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            frameworkConfig.SetValue(FrameworkSetting.WindowMode, WindowMode.Fullscreen);

            Add(new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Alpha = 0.1f
            });

            Dropdown<Size> resolutionDropdown;

            Add(new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    infoText = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "Resolution: "
                            },
                            resolutionDropdown = new BasicDropdown<Size>
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Width = 200,
                                Items = new[]
                                {
                                    new Size(1280, 720),
                                    new Size(1920, 1080)
                                },
                            }
                        }
                    },
                    new BasicButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 200,
                        Height = 30,
                        Text = "Take screenshot",
                        FlashColour = FrameworkColour.BlueGreen.Lighten(0.2f),
                        Action = takeScreenshot
                    }
                }
            });

            configWindowSize = frameworkConfig.GetBindable<Size>(FrameworkSetting.SizeFullscreen);
            configWindowSize.BindValueChanged(s => resolutionDropdown.Current.Value = s.NewValue, true);
            resolutionDropdown.Current.BindValueChanged(s => frameworkConfig.SetValue(FrameworkSetting.SizeFullscreen, s.NewValue));
        }

        private void takeScreenshot()
        {
            Host.TakeScreenshotAsync().WaitSafely();
        }

        protected override void Update()
        {
            base.Update();
            infoText.Text = $"Renderer: {Host.RendererInfo}, Display: {Host.Window.CurrentDisplayMode.Value.ToString()}";
        }
    }
}
