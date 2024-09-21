// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Testing.Drawables.Steps;

namespace osu.Framework.Testing
{
    public abstract partial class TestScene : AdhocTestScene
    {
        /// <summary>
        /// Tests any steps and assertions in the constructor of this <see cref="TestScene"/>.
        /// This test must run before any other tests, as it relies on <see cref="TestScene.StepsContainer"/> not being cleared and not having any elements.
        /// </summary>
        [Test]
        public Task TestConstructor() => Task.CompletedTask;

        public override void RunAllSteps(Action? onCompletion = null, Action<Exception>? onError = null, Func<StepButton, bool>? stopCondition = null, StepButton? startFromStep = null)
        {
            Logger.Log($@"🔶 Test:  {TestContext.Current?.TestDetails.DisplayName}");
            base.RunAllSteps(onCompletion, onError, stopCondition, startFromStep);
        }

        protected void AddUntilStep(string? description, Func<Task> assertion) => Scheduler.Add(() =>
        {
            Func<(bool status, string message)> func = wrap(assertion);

            StepsContainer.Add(new UntilStepButton(() => func().status, AddStepsAsSetupSteps, () => func().message)
            {
                Text = description ?? "Until",
            });
        }, false);

        protected void AddAssert(string description, Func<Task> assertion, string? extendedDescription = null)
        {
            StackTrace callStack = new StackTrace(1);

            Scheduler.Add(() =>
            {
                Func<(bool status, string message)> func = wrap(assertion);

                StepsContainer.Add(new AssertButton(AddStepsAsSetupSteps, () => func().message)
                {
                    Text = description,
                    ExtendedDescription = extendedDescription,
                    CallStack = callStack,
                    Assertion = () => func().status
                });
            }, false);
        }

        private static Func<(bool status, string message)> wrap(Func<Task> assert) => () =>
        {
            return assertAndWait(assert).GetResultSafely();

            static async Task<(bool status, string message)> assertAndWait(Func<Task> assert)
            {
                try
                {
                    await assert().ConfigureAwait(false);
                    return (true, string.Empty);
                }
                catch (TUnit.Assertions.Exceptions.AssertionException e)
                {
                    return (false, e.Message);
                }
            }
        };
    }
}
