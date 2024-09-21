// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Testing.Drawables.Steps;
using TUnit.Core.Executors;

namespace osu.Framework.Testing
{
    public abstract partial class TestScene : AdhocTestScene
    {
        public override void RunAllSteps(Action? onCompletion = null, Action<Exception>? onError = null, Func<StepButton, bool>? stopCondition = null, StepButton? startFromStep = null)
        {
            Logger.Log($@"🔶 Test:  {TestContext.Current?.TestDetails.DisplayName}");
            base.RunAllSteps(onCompletion, onError, stopCondition, startFromStep);
        }

        /// <summary>
        /// Tests any steps and assertions in the constructor of this <see cref="TestScene"/>.
        /// This test must run before any other tests, as it relies on <see cref="TestScene.StepsContainer"/> not being cleared and not having any elements.
        /// </summary>
        [Test, HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public Task TestConstructor()
        {
            ThreadSafety.EnsureUpdateThread();
            return Task.CompletedTask;
        }

        [Before(Test), HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public new void RunSetUpSteps()
        {
            ThreadSafety.EnsureUpdateThread();
            base.RunSetUpSteps();
        }

        [After(Test), HookExecutor<TestSceneExecutor>, TestExecutor<TestSceneExecutor>]
        public new void RunTearDownSteps()
        {
            ThreadSafety.EnsureUpdateThread();

            base.RunTearDownSteps();
            base.RunAfterTest();

            if (FrameworkEnvironment.ForceTestGC)
            {
                // Force any unobserved exceptions to fire against the current test run.
                // Without this they could be delayed until a future test scene is running, making tracking down the cause difficult.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
