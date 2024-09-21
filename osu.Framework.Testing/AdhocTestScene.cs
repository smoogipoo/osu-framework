// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Threading;
using osuTK.Graphics;
using Logger = osu.Framework.Logging.Logger;
using Vector2 = osuTK.Vector2;

namespace osu.Framework.Testing
{
    public abstract partial class AdhocTestScene : Container
    {
        public readonly FillFlowContainer<Drawable> StepsContainer;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        protected internal virtual ITestSceneTestRunner CreateRunner() => new TestSceneTestRunner();

        /// <summary>
        /// Delay between invoking two <see cref="StepButton"/>s in automatic runs.
        /// </summary>
        protected virtual double TimePerAction => 200;

        /// <summary>
        /// Whether to automatically run the the first actual <see cref="StepButton"/> (one that is not part of <see cref="SetUpAttribute">[SetUp]</see> or <see cref="SetUpStepsAttribute">[SetUpSteps]</see>)
        /// when the test is first loaded.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>. Should be set to <c>false</c> if the first step in the first <see cref="TestAttribute">test</see> has unwanted-by-default behaviour.
        /// </remarks>
        public virtual bool AutomaticallyRunFirstStep => true;

        internal GameHost Host;

        private readonly Box backgroundFill;

        /// <summary>
        /// A nested game instance, if added via <see cref="AddGame"/>.
        /// </summary>
        private Game nestedGame;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            // SetupGameHost won't be run for interactive runs so we need to populate this from DI.
            Host ??= host;
        }

        /// <summary>
        /// Add a full game instance in a nested state for visual testing.
        /// </summary>
        /// <remarks>
        /// Any previous game added via this method will be disposed if called multiple times.
        /// </remarks>
        /// <param name="game">The game to add.</param>
        protected void AddGame([NotNull] Game game)
        {
            ArgumentNullException.ThrowIfNull(game);

            exitNestedGame();

            nestedGame = game;
            nestedGame.SetHost(Host);

            base.Add(nestedGame);
        }

        public override void Add(Drawable drawable)
        {
            ArgumentNullException.ThrowIfNull(drawable);

            if (drawable is Game)
                throw new InvalidOperationException($"Use {nameof(AddGame)} when testing a game instance.");

            base.Add(drawable);
        }

        protected override void AddInternal(Drawable drawable)
        {
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Add)} instead.");
        }

        protected internal override void ClearInternal(bool disposeChildren = true) =>
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Clear)} instead.");

        protected internal override bool RemoveInternal(Drawable drawable, bool disposeImmediately) =>
            throw new InvalidOperationException($"Modifying {nameof(InternalChildren)} will cause critical failure. Use {nameof(Remove)} instead.");

        protected AdhocTestScene()
        {
            Name = RemovePrefix(GetType().ReadableName());

            RelativeSizeAxes = Axes.Both;
            Masking = true;

            base.AddInternal(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = new Color4(25, 25, 25, 255),
                        RelativeSizeAxes = Axes.Y,
                        Width = steps_width,
                    },
                    scroll = new BasicScrollContainer
                    {
                        Width = steps_width,
                        Depth = float.MinValue,
                        RelativeSizeAxes = Axes.Y,
                        Child = StepsContainer = new FillFlowContainer<Drawable>
                        {
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(3),
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding(10),
                            Child = new SpriteText
                            {
                                Font = FrameworkFont.Condensed.With(size: 16),
                                Text = Name,
                                Margin = new MarginPadding { Bottom = 5 },
                            }
                        },
                    },
                    new Container
                    {
                        Masking = true,
                        Padding = new MarginPadding
                        {
                            Left = steps_width + padding,
                            Right = padding,
                            Top = padding,
                            Bottom = padding,
                        },
                        RelativeSizeAxes = Axes.Both,
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                backgroundFill = new Box
                                {
                                    Colour = Color4.Black,
                                    RelativeSizeAxes = Axes.Both,
                                },
                                content = new DrawFrameRecordingContainer
                                {
                                    Masking = true,
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        },
                    },
                }
            });
        }

        private const float steps_width = 180;
        private const float padding = 0;

        private int actionIndex;
        private int actionRepetition;
        private ScheduledDelegate stepRunner;
        private readonly ScrollContainer<Drawable> scroll;

        public virtual void RunAllSteps(Action onCompletion = null, Action<Exception> onError = null, Func<StepButton, bool> stopCondition = null, StepButton startFromStep = null)
        {
            // schedule once as we want to ensure we have run our LoadComplete before attempting to execute steps.
            // a user may be adding a step in LoadComplete.
            Schedule(() =>
            {
                stepRunner?.Cancel();
                foreach (var step in StepsContainer.FlowingChildren.OfType<StepButton>())
                    step.Reset();

                actionIndex = startFromStep != null ? StepsContainer.IndexOf(startFromStep) + 1 : -1;
                actionRepetition = 0;
                runNextStep(onCompletion, onError, stopCondition);
            });
        }

        private StepButton loadableStep => actionIndex >= 0 ? StepsContainer.Children.ElementAtOrDefault(actionIndex) as StepButton : null;

        private void runNextStep(Action onCompletion, Action<Exception> onError, Func<StepButton, bool> stopCondition)
        {
            try
            {
                if (loadableStep != null)
                {
                    if (actionRepetition == 0)
                        Logger.Log($"🔸 Step #{actionIndex + 1} {loadableStep.Text}");

                    scroll.ScrollIntoView(loadableStep);
                    loadableStep.PerformStep();
                }
            }
            catch (Exception e)
            {
                Logger.Log(actionRepetition > 0
                    ? $"💥 Failed (on attempt {actionRepetition:#,0})"
                    : "💥 Failed");

                LoadingComponentsLogger.LogAndFlush();
                onError?.Invoke(e);
                return;
            }

            actionRepetition++;

            if (actionRepetition > (loadableStep?.RequiredRepetitions ?? 1) - 1)
            {
                if (actionIndex >= 0 && actionRepetition > 1)
                    Logger.Log($"✔️ {actionRepetition} repetitions");

                actionIndex++;
                actionRepetition = 0;

                if (loadableStep != null && stopCondition?.Invoke(loadableStep) == true)
                    return;
            }

            if (actionIndex > StepsContainer.Children.Count - 1)
            {
                Logger.Log($"✅ {GetType().ReadableName()} completed");
                onCompletion?.Invoke();
                return;
            }

            if (Parent != null)
                stepRunner = Scheduler.AddDelayed(() => runNextStep(onCompletion, onError, stopCondition), TimePerAction);
        }

        public void AddStep(StepButton step) => Scheduler.Add(() => StepsContainer.Add(step), false);

        internal bool AddStepsAsSetupSteps { get; private set; }

        public void ChangeBackgroundColour(ColourInfo colour)
            => backgroundFill.FadeColour(colour, 200, Easing.OutQuint);

        public StepButton AddStep(string description, Action action)
        {
            var step = new SingleStepButton(AddStepsAsSetupSteps)
            {
                Text = description,
                Action = action
            };

            AddStep(step);

            return step;
        }

        public LabelStep AddLabel(string description)
        {
            var step = new LabelStep
            {
                Text = description,
            };

            step.Action = () =>
            {
                Logger.Log($@"💨 {this} {description}");

                // kinda hacky way to avoid this doesn't get triggered by automated runs.
                if (step.IsHovered)
                    RunAllSteps(startFromStep: step, stopCondition: s => s is LabelStep);
            };

            AddStep(step);

            return step;
        }

        protected void AddRepeatStep(string description, Action action, int invocationCount) => Scheduler.Add(() =>
        {
            StepsContainer.Add(new RepeatStepButton(action, invocationCount, AddStepsAsSetupSteps)
            {
                Text = description,
            });
        }, false);

        protected void AddToggleStep(string description, Action<bool> action) => Scheduler.Add(() =>
        {
            StepsContainer.Add(new ToggleStepButton(action)
            {
                Text = description
            });
        }, false);

        protected void AddUntilStep(string description, Func<bool> waitUntilTrueDelegate) => Scheduler.Add(() =>
        {
            StepsContainer.Add(new UntilStepButton(waitUntilTrueDelegate, AddStepsAsSetupSteps)
            {
                Text = description ?? @"Until",
            });
        }, false);

        protected void AddWaitStep(string description, int waitCount) => Scheduler.Add(() =>
        {
            StepsContainer.Add(new RepeatStepButton(() => { }, waitCount, AddStepsAsSetupSteps)
            {
                Text = description ?? @"Wait",
            });
        }, false);

        protected void AddSliderStep<T>(string description, T min, T max, T start, Action<T> valueChanged) where T : struct, INumber<T>, IMinMaxValue<T> => Scheduler.Add(() =>
        {
            StepsContainer.Add(new StepSlider<T>(description, min, max, start)
            {
                ValueChanged = valueChanged,
            });
        }, false);

        protected void AddAssert(string description, Func<bool> assert, string extendedDescription = null) => Scheduler.Add(() =>
        {
            StepsContainer.Add(new AssertButton(AddStepsAsSetupSteps)
            {
                Text = description,
                ExtendedDescription = extendedDescription,
                CallStack = new StackTrace(1),
                Assertion = assert,
            });
        }, false);

        internal void RunSetUpSteps()
        {
            AddStepsAsSetupSteps = true;
            foreach (var method in ReflectionUtils.GetMethodsWithAttribute(GetType(), typeof(SetUpStepsAttribute), true))
                method.Invoke(this, null);
            AddStepsAsSetupSteps = false;
        }

        internal void RunTearDownSteps()
        {
            foreach (var method in ReflectionUtils.GetMethodsWithAttribute(GetType(), typeof(TearDownStepsAttribute), true))
                method.Invoke(this, null);
        }

        /// <summary>
        /// Remove the "TestScene" prefix from a name.
        /// </summary>
        /// <param name="name"></param>
        public static string RemovePrefix(string name)
        {
            return name.Replace("TestCase", string.Empty) // TestScene used to be called TestCase. This handles consumer projects which haven't updated their naming for the near future.
                       .Replace(nameof(AdhocTestScene), string.Empty);
        }

        internal virtual void RunAfterTest()
        {
        }

        private void exitNestedGame()
        {
            nestedGame?.Parent?.RemoveInternal(nestedGame, true);
        }

        internal class TestSceneHost : TestRunHeadlessGameHost
        {
            private readonly AdhocTestScene testScene;

            public TestSceneHost(AdhocTestScene testScene, string name)
                : base(name, new HostOptions())
            {
                this.testScene = testScene;
            }

            protected override void PerformExit(bool immediately)
            {
                // Base call is blocked so that nested game instances can't end the testing process.
                testScene.exitNestedGame();
            }

            public void ExitFromRunner() => base.PerformExit(false);
        }
    }
}
