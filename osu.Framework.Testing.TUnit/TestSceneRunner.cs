// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Development;

namespace osu.Framework.Testing
{
    internal static class TestSceneRunner
    {
        private static readonly ConcurrentDictionary<TestContext, TestSceneContext> host_contexts = new ConcurrentDictionary<TestContext, TestSceneContext>();

        [BeforeEvery(Assembly)]
        public static Task PrepareAssembly(AssemblyHookContext context)
        {
            DebugUtils.IsTestRunning = true;
            RuntimeInfo.EntryAssembly = context.Assembly;
            return Task.CompletedTask;
        }

        [BeforeEvery(Test)]
        public static async Task PrepareGameHost(TestContext context)
        {
            if (context.TestDetails.ClassInstance is not TestScene testScene)
                return;

            var hostContext = new TestSceneContext(testScene);

            host_contexts[context] = hostContext;
            await hostContext.Start().ConfigureAwait(false);

            testScene.RunSetUpSteps();
            await hostContext.CheckForErrors().ConfigureAwait(false);
        }

        [AfterEvery(Test)]
        public static async Task ShutdownGameHost(TestContext context)
        {
            if (context.TestDetails.ClassInstance is not TestScene testScene)
                return;

            var hostContext = host_contexts[context];

            try
            {
                testScene.RunTearDownSteps();
                await hostContext.CheckForErrors().ConfigureAwait(false);

                hostContext.Runner.RunTestBlocking(testScene);
                await hostContext.CheckForErrors().ConfigureAwait(false);

                if (FrameworkEnvironment.ForceTestGC)
                {
                    // Force any unobserved exceptions to fire against the current test run.
                    // Without this they could be delayed until a future test scene is running, making tracking down the cause difficult.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                testScene.RunAfterTest();
                await hostContext.CheckForErrors().ConfigureAwait(false);
            }
            catch (AssertionException exc)
            {
                throw new TUnit.Assertions.Exceptions.AssertionException(null, exc);
            }
            finally
            {
                await hostContext.Stop().ConfigureAwait(false);
                host_contexts.Remove(context, out _);
            }
        }
    }
}
