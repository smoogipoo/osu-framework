// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace osu.Framework.Testing
{
    public class TestSceneExecutor : IHookExecutor, ITestExecutor
    {
        public async Task ExecuteBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteTest(TestContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }

        public async Task ExecuteAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
        {
            if (TestSceneRunner.GetCurrentContext() is TestSceneContext testSceneContext)
                await testSceneContext.RunOnUpdateThread(action).ConfigureAwait(false);
            else
                await action().ConfigureAwait(false);
        }
    }
}
