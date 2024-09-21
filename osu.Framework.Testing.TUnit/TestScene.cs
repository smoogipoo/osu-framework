// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
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
    }
}
