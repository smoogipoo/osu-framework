// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Framework.Testing.Drawables.Steps;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Framework.Testing
{
    [TestFixture]
    [UseTestSceneRunner]
    public abstract partial class TestScene : AdhocTestScene
    {
        /// <summary>
        /// Tests any steps and assertions in the constructor of this <see cref="TestScene"/>.
        /// This test must run before any other tests, as it relies on <see cref="TestScene.StepsContainer"/> not being cleared and not having any elements.
        /// </summary>
        [Test, Order(int.MinValue)]
        public void TestConstructor()
        {
        }

        public override void RunAllSteps(Action? onCompletion = null, Action<Exception>? onError = null, Func<StepButton, bool>? stopCondition = null, StepButton? startFromStep = null)
        {
            Logger.Log($@"🔶 Test:  {TestContext.CurrentContext.Test.Name}");
            base.RunAllSteps(onCompletion, onError, stopCondition, startFromStep);
        }

        protected void AddUntilStep<T>(string? description, ActualValueDelegate<T> actualValue, Func<IResolveConstraint> constraint) => Scheduler.Add(() =>
        {
            ConstraintResult? lastResult = null;

            StepsContainer.Add(
                new UntilStepButton(
                    () =>
                    {
                        lastResult = constraint().Resolve().ApplyTo(actualValue());
                        return lastResult.IsSuccess;
                    },
                    AddStepsAsSetupSteps,
                    () =>
                    {
                        if (lastResult == null)
                            return string.Empty;

                        var writer = new TextMessageWriter(string.Empty);
                        lastResult.WriteMessageTo(writer);
                        return writer.ToString().TrimStart();
                    })
                {
                    Text = description ?? "Until",
                });
        }, false);

        protected void AddAssert<T>(string description, ActualValueDelegate<T> actualValue, Func<IResolveConstraint> constraint, string? extendedDescription = null)
        {
            StackTrace callStack = new StackTrace(1);

            Scheduler.Add(() =>
            {
                ConstraintResult? lastResult = null;

                StepsContainer.Add(new AssertButton(AddStepsAsSetupSteps, () =>
                {
                    if (lastResult == null)
                        return string.Empty;

                    var writer = new TextMessageWriter(string.Empty);
                    lastResult.WriteMessageTo(writer);
                    return writer.ToString().TrimStart();
                })
                {
                    Text = description,
                    ExtendedDescription = extendedDescription,
                    CallStack = callStack,
                    Assertion = () =>
                    {
                        lastResult = constraint().Resolve().ApplyTo(actualValue());
                        return lastResult.IsSuccess;
                    }
                });
            }, false);
        }

        private Task? runTask;
        private ITestSceneTestRunner? runner;

        [OneTimeSetUp]
        public void SetupGameHostForNUnit()
        {
            Host = new TestSceneHost(this, $"{GetType().Name}-{Guid.NewGuid()}");
            runner = CreateRunner();

            if (!(runner is Game game))
                throw new InvalidCastException($"The test runner must be a {nameof(Game)}.");

            runTask = Task.Factory.StartNew(() => Host.Run(game), TaskCreationOptions.LongRunning);

            while (!game.IsLoaded)
            {
                checkForErrors();
                Thread.Sleep(10);
            }
        }

        [OneTimeTearDown]
        public void DestroyGameHostFromNUnit()
        {
            ((TestSceneHost)Host).ExitFromRunner();

            try
            {
                runTask!.WaitSafely();
            }
            finally
            {
                Host.Dispose();

                try
                {
                    // clean up after each run
                    Host.Storage.DeleteDirectory(string.Empty);
                }
                catch
                {
                }
            }
        }

        private void checkForErrors()
        {
            if (Host.ExecutionState == ExecutionState.Stopping)
                runTask!.WaitSafely();

            if (runTask!.Exception != null)
                throw runTask.Exception;
        }

        private class UseTestSceneRunnerAttribute : TestActionAttribute
        {
            public override void BeforeTest(ITest test)
            {
                if (test.Fixture is not TestScene testScene)
                    return;

                // Since the host is created in OneTimeSetUp, all game threads will have the fixture's execution context
                // This is undesirable since each test is run using those same threads, so we must make sure the execution context
                // for the game threads refers to the current _test_ execution context for each test
                var executionContext = TestExecutionContext.CurrentContext;

                foreach (var thread in testScene.Host.Threads)
                {
                    thread.Scheduler.Add(() =>
                    {
                        TestExecutionContext.CurrentContext.CurrentResult = executionContext.CurrentResult;
                        TestExecutionContext.CurrentContext.CurrentTest = executionContext.CurrentTest;
                        TestExecutionContext.CurrentContext.CurrentCulture = executionContext.CurrentCulture;
                        TestExecutionContext.CurrentContext.CurrentPrincipal = executionContext.CurrentPrincipal;
                        TestExecutionContext.CurrentContext.CurrentRepeatCount = executionContext.CurrentRepeatCount;
                        TestExecutionContext.CurrentContext.CurrentUICulture = executionContext.CurrentUICulture;
                    });
                }

                if (TestContext.CurrentContext.Test.MethodName != nameof(TestConstructor))
                    testScene.Schedule(() => testScene.StepsContainer.Clear());

                testScene.RunSetUpSteps();
            }

            public override void AfterTest(ITest test)
            {
                if (test.Fixture is not TestScene testScene)
                    return;

                testScene.RunTearDownSteps();

                testScene.checkForErrors();
                testScene.runner!.RunTestBlocking(testScene);
                testScene.checkForErrors();

                if (FrameworkEnvironment.ForceTestGC)
                {
                    // Force any unobserved exceptions to fire against the current test run.
                    // Without this they could be delayed until a future test scene is running, making tracking down the cause difficult.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                testScene.RunAfterTest();
            }

            public override ActionTargets Targets => ActionTargets.Test;
        }
    }
}
