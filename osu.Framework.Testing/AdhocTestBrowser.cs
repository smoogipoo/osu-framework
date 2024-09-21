// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Framework.Testing
{
    [Cached]
    public abstract partial class AdhocTestBrowser : KeyBindingContainer<TestBrowserAction>, IKeyBindingHandler<TestBrowserAction>, IHandleGlobalKeyboardInput
    {
        public AdhocTestScene CurrentTest { get; private set; }

        private BasicTextBox searchTextBox;
        private SearchContainer<TestGroupButton> leftFlowContainer;
        private Container testContentContainer;
        private Container hotReloadNotice;

        public readonly List<Type> TestTypes = new List<Type>();

        private ConfigManager<TestBrowserSetting> config;

        private bool interactive;

        /// <summary>
        /// Creates a new TestBrowser that displays the TestCases of every assembly that start with either "osu" or the specified namespace (if it isn't null)
        /// </summary>
        /// <param name="assemblyNamespace">Assembly prefix which is used to match assemblies whose tests should be displayed</param>
        protected AdhocTestBrowser(string assemblyNamespace = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(n =>
            {
                Debug.Assert(n.FullName != null);
                return n.FullName.StartsWith("osu", StringComparison.Ordinal) || (assemblyNamespace != null && n.FullName.StartsWith(assemblyNamespace, StringComparison.Ordinal));
            }).ToList();

            //we want to build the lists here because we're interested in the assembly we were *created* on.
            foreach (Assembly asm in assemblies)
            {
                foreach (Type type in asm.GetLoadableTypes().Where(isValidVisualTest))
                    TestTypes.Add(type);
            }

            TestTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        private bool isValidVisualTest(Type t) =>
            t.IsSubclassOf(typeof(AdhocTestScene)) && !t.IsAbstract && t.IsPublic && !t.GetCustomAttributes<HeadlessTestAttribute>().Any() && t.IsSupportedOnCurrentOSPlatform();

        internal readonly BindableDouble PlaybackRate = new BindableDouble(1) { MinValue = 0, MaxValue = 2, Default = 1 };
        internal readonly Bindable<bool> RunAllSteps = new Bindable<bool>();
        internal readonly Bindable<RecordState> RecordState = new Bindable<RecordState>();
        internal readonly BindableInt CurrentFrame = new BindableInt { MinValue = 0, MaxValue = 0 };

        private Container leftContainer;
        private Container mainContainer;

        private const float test_list_width = 200;

        private readonly BindableDouble audioRateAdjust = new BindableDouble(1);

        [BackgroundDependencyLoader]
        private void load(Storage storage, GameHost host, AudioManager audio)
        {
            interactive = host.Window != null;
            config = new TestBrowserConfig(storage);

            audio.AddAdjustment(AdjustableProperty.Frequency, audioRateAdjust);

            var rateAdjustClock = new StopwatchClock(true);
            var framedClock = new FramedClock(rateAdjustClock);

            Children = new Drawable[]
            {
                mainContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = test_list_width },
                    Children = new Drawable[]
                    {
                        new SafeAreaContainer
                        {
                            SafeAreaOverrideEdges = Edges.Right | Edges.Bottom,
                            RelativeSizeAxes = Axes.Both,
                            Child = testContentContainer = new Container
                            {
                                Clock = framedClock,
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Top = 50 },
                                Child = hotReloadNotice = new Container
                                {
                                    Alpha = 0,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Depth = float.MinValue,
                                    CornerRadius = 5,
                                    AutoSizeAxes = Axes.Both,
                                    Colour = Color4.YellowGreen,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Black,
                                        },
                                        new SpriteText
                                        {
                                            Font = FrameworkFont.Regular.With(size: 30),
                                            Text = @"Hot reload!",
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Margin = new MarginPadding(5),
                                        }
                                    },
                                }
                            }
                        },
                        new TestBrowserToolbar
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 50,
                        },
                    }
                },
                leftContainer = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(test_list_width, 1),
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new SafeAreaContainer
                        {
                            SafeAreaOverrideEdges = Edges.Left | Edges.Top | Edges.Bottom,
                            RelativeSizeAxes = Axes.Both,
                            Child = new Box
                            {
                                Colour = FrameworkColour.GreenDark,
                                RelativeSizeAxes = Axes.Both
                            }
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Vertical,
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                searchTextBox = new TestBrowserTextBox
                                {
                                    Height = 25,
                                    RelativeSizeAxes = Axes.X,
                                    PlaceholderText = "type to search",
                                    Depth = -1,
                                },
                                new BasicScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = false,
                                    Child = leftFlowContainer = new SearchContainer<TestGroupButton>
                                    {
                                        AllowNonContiguousMatching = true,
                                        Padding = new MarginPadding { Top = 3, Bottom = 20 },
                                        Direction = FillDirection.Vertical,
                                        AutoSizeAxes = Axes.Y,
                                        RelativeSizeAxes = Axes.X,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            searchTextBox.OnCommit += delegate
            {
                var firstTest = leftFlowContainer.Where(b => b.IsPresent).SelectMany(b => b.FilterableChildren).OfType<TestSubButton>()
                                                 .FirstOrDefault(b => b.MatchingFilter)?.TestType;
                if (firstTest != null)
                    LoadTest(firstTest);
            };

            searchTextBox.Current.ValueChanged += e => leftFlowContainer.SearchTerm = e.NewValue;

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += compileFinished;

            RunAllSteps.BindValueChanged(_ => runTests(null));
            PlaybackRate.BindValueChanged(e =>
            {
                rateAdjustClock.Rate = e.NewValue;
                audioRateAdjust.Value = e.NewValue;
            }, true);

            updateTestList();
        }

        private void updateTestList()
        {
            leftFlowContainer.Clear();

            //Add buttons for each TestCase.
            string namespacePrefix = TestTypes.Select(t => t.Namespace).GetCommonPrefix();

            leftFlowContainer.AddRange(TestTypes.GroupBy(
                                                    t =>
                                                    {
                                                        string group = t.Namespace?.AsSpan(namespacePrefix.Length).TrimStart('.').ToString();
                                                        return string.IsNullOrWhiteSpace(group) ? "Ungrouped" : group;
                                                    },
                                                    t => t,
                                                    (group, types) => new TestGroup { Name = group, TestTypes = types.ToArray() }
                                                ).OrderBy(g => g.Name)
                                                .Select(t => new TestGroupButton(type => LoadTest(type), t)));
        }

        private void compileFinished(Type[] updatedTypes) => Schedule(() =>
        {
            if (CurrentTest == null)
                return;

            try
            {
                LoadTest(CurrentTest.GetType(), isHotReload: true);

                hotReloadNotice
                    .FadeIn(100).Then()
                    .FadeOutFromOne(500, Easing.InQuint);
                hotReloadNotice.FadeColour(Color4.YellowGreen, 100);
            }
            catch (Exception e)
            {
                compileFailed(e);
            }
        });

        private void compileFailed(Exception ex) => Schedule(() =>
        {
            Logger.Error(ex, "Error loading test after hot reload.");

            hotReloadNotice
                .FadeIn(100).Then()
                .FadeOutFromOne(500, Easing.InQuint);
            hotReloadNotice.FadeColour(Color4.Red, 100);
        });

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (CurrentTest == null)
            {
                string lastTest = config.Get<string>(TestBrowserSetting.LastTest);

                var foundTest = TestTypes.Find(t => t.FullName == lastTest);

                LoadTest(foundTest);
            }
        }

        private void toggleTestList()
        {
            if (leftContainer.Width > 0)
            {
                leftContainer.Width = 0;
                mainContainer.Padding = new MarginPadding();
            }
            else
            {
                leftContainer.Width = test_list_width;
                mainContainer.Padding = new MarginPadding { Left = test_list_width };
            }
        }

        public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F }, TestBrowserAction.Search),
            new KeyBinding(new[] { InputKey.Control, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Super, InputKey.F }, TestBrowserAction.Search), // for macOS
            new KeyBinding(new[] { InputKey.Super, InputKey.R }, TestBrowserAction.Reload), // for macOS

            new KeyBinding(new[] { InputKey.Control, InputKey.H }, TestBrowserAction.ToggleTestList),
        };

        public bool OnPressed(KeyBindingPressEvent<TestBrowserAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case TestBrowserAction.Search:
                    if (leftContainer.Width == 0) toggleTestList();
                    GetContainingFocusManager().AsNonNull().ChangeFocus(searchTextBox);
                    return true;

                case TestBrowserAction.Reload:
                    LoadTest(CurrentTest.GetType());
                    return true;

                case TestBrowserAction.ToggleTestList:
                    toggleTestList();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<TestBrowserAction> e)
        {
        }

        public void LoadTest(Type testType = null, Action onCompletion = null, bool isHotReload = false)
        {
            if (CurrentTest?.Parent != null)
            {
                testContentContainer.Remove(CurrentTest.Parent, true);
            }

            CurrentTest = null;

            if (testType == null && TestTypes.Count > 0)
                testType = TestTypes[0];

            config.SetValue(TestBrowserSetting.LastTest, testType?.FullName ?? string.Empty);

            if (testType == null)
                return;

            var newTest = (AdhocTestScene)Activator.CreateInstance(testType);

            Debug.Assert(newTest != null);

            CurrentTest = newTest;
            CurrentTest.OnLoadComplete += _ => Schedule(() => finishLoad(newTest, onCompletion));

            updateButtons();
            resetRecording();

            testContentContainer.Add(new ErrorCatchingDelayedLoadWrapper(CurrentTest, isHotReload)
            {
                OnCaughtError = compileFailed
            });
        }

        private void resetRecording()
        {
            CurrentFrame.Value = 0;
            if (RecordState.Value == Testing.RecordState.Stopped)
                RecordState.Value = Testing.RecordState.Normal;
        }

        private void finishLoad(AdhocTestScene newTest, Action onCompletion)
        {
            if (CurrentTest != newTest)
            {
                if (newTest.Parent != null)
                {
                    // There could have been multiple loads fired after us. In such a case we want to silently remove ourselves.
                    testContentContainer.Remove(newTest.Parent, true);
                }

                return;
            }

            updateButtons();

            var methods = newTest.GetType().GetMethods();

            var soloTests = methods.Where(m => m.GetCustomAttribute(typeof(SoloAttribute), false) != null).ToArray();
            if (soloTests.Any())
                methods = soloTests;

            AddStepsForMethods(newTest, methods);

            runTests(onCompletion);
            updateButtons();
        }

        protected abstract void AddStepsForMethods(AdhocTestScene newTest, IEnumerable<MethodInfo> methods);

        private void runTests(Action onCompletion)
        {
            int actualStepCount = 0;
            CurrentTest.RunAllSteps(onCompletion, e => Logger.Log($@"Error on step: {e}"), s =>
            {
                if (!interactive || RunAllSteps.Value)
                    return false;

                if (actualStepCount > 0)
                    // stop once one actual step has been run.
                    return true;

                if (!s.IsSetupStep && !(s is LabelStep))
                {
                    actualStepCount++;

                    // immediately stop if the test scene has requested it.
                    if (!CurrentTest.AutomaticallyRunFirstStep)
                        return true;
                }

                return false;
            });
        }

        private void updateButtons()
        {
            foreach (var b in leftFlowContainer.Children)
                b.Current = CurrentTest.GetType();
        }

        private partial class ErrorCatchingDelayedLoadWrapper : DelayedLoadWrapper
        {
            private readonly bool catchErrors;
            private bool hasCaught;

            public Action<Exception> OnCaughtError;

            public ErrorCatchingDelayedLoadWrapper(Drawable content, bool catchErrors)
                : base(content, 0)
            {
                this.catchErrors = catchErrors;
            }

            public override bool UpdateSubTree()
            {
                try
                {
                    return base.UpdateSubTree();
                }
                catch (Exception e)
                {
                    if (!catchErrors)
                        throw;

                    // without this we will enter an infinite loading loop (DelayedLoadWrapper will see the child removed below and retry).
                    hasCaught = true;

                    OnCaughtError?.Invoke(e);
                    RemoveInternal(Content, true);
                }

                return false;
            }

            protected override bool ShouldLoadContent => !hasCaught;
        }

        private partial class TestBrowserTextBox : BasicTextBox
        {
            protected override float LeftRightPadding => TestButtonBase.LEFT_TEXT_PADDING;

            public TestBrowserTextBox()
            {
                FontSize = 14f;
            }
        }
    }

    internal enum RecordState
    {
        /// <summary>
        /// The game is playing back normally.
        /// </summary>
        Normal,

        /// <summary>
        /// Drawn game frames are currently being recorded.
        /// </summary>
        Recording,

        /// <summary>
        /// The default game playback is stopped, recorded frames are being played back.
        /// </summary>
        Stopped
    }

    public enum TestBrowserAction
    {
        ToggleTestList,
        Reload,
        Search
    }
}
