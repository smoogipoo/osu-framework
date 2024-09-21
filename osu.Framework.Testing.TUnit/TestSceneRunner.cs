// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        }

        [AfterEvery(Test)]
        public static async Task ShutdownGameHost(TestContext context)
        {
            if (context.TestDetails.ClassInstance is not TestScene)
                return;

            var hostContext = host_contexts[context];
            host_contexts.Remove(context, out _);

            await hostContext.CheckForErrors().ConfigureAwait(false);
            await hostContext.Stop().ConfigureAwait(false);
        }

        public static TestSceneContext? GetCurrentContext() => host_contexts.GetValueOrDefault(TestContext.Current!);
    }
}
