// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Platform;
using TUnit.Core.Interfaces;

namespace osu.Framework.Testing
{
    internal static class TestRunner
    {
        private static readonly ConcurrentDictionary<TestContext, HostContext> host_contexts = new ConcurrentDictionary<TestContext, HostContext>();

        [BeforeEvery(HookType.Assembly)]
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

            var hostContext = new HostContext(testScene);
            host_contexts[context] = hostContext;
            await hostContext.Start();

            // Todo: HACK HACK HACK!!!
            // Set a custom executor that uses the update thread's synchronisation context.
            object internalDiscoveredTest = typeof(TestContext).GetProperty("InternalDiscoveredTest", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(context)!;
            internalDiscoveredTest.GetType().GetProperty("TestExecutor", BindingFlags.Instance | BindingFlags.Public)!.SetValue(internalDiscoveredTest, hostContext.TestExecutor);
        }

        [AfterEvery(Test)]
        public static async Task ShutdownGameHost(TestContext context)
        {
            if (context.TestDetails.ClassInstance is not TestScene testScene)
                return;

            var hostContext = host_contexts[context];
            host_contexts.Remove(context, out _);

            await hostContext.RunOnUpdateThread(() => Task.Run(() => testScene.RunTearDownSteps()));
            await hostContext.CheckForErrors();

            if (FrameworkEnvironment.ForceTestGC)
            {
                // Force any unobserved exceptions to fire against the current test run.
                // Without this they could be delayed until a future test scene is running, making tracking down the cause difficult.
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            await hostContext.RunOnUpdateThread(() => Task.Run(() => testScene.RunAfterTest()));
            await hostContext.CheckForErrors();
            await hostContext.Stop();
        }

        private record HostContext
        {
            public readonly ITestExecutor TestExecutor;
            public readonly AdhocTestScene.TestSceneHost Host;
            public readonly ITestSceneTestRunner Runner;

            private readonly Game game;
            private readonly Thread thread;

            private Exception? hostException;

            public HostContext(TestScene testScene)
            {
                TestExecutor = new UpdateThreadTestExecutor(this);
                Host = new AdhocTestScene.TestSceneHost(testScene, $"{testScene.GetType().Name}-{Guid.NewGuid()}");
                Runner = testScene.CreateRunner();

                if (Runner is not Game)
                    throw new InvalidCastException($"The test runner must be a {nameof(Game)}.");

                game = (Game)Runner;
                game.OnLoadComplete += _ => game.Add(testScene);

                thread = new Thread(runHost);
            }

            private void runHost()
            {
                try
                {
                    Host.Run(game);
                }
                catch (Exception ex)
                {
                    hostException = ex;
                }
            }

            public async Task Start()
            {
                ThreadSafety.EnsureNotUpdateThread();

                thread.Start();

                while (!game.IsLoaded)
                {
                    await CheckForErrors();
                    await Task.Delay(10);
                }
            }

            public async Task Stop()
            {
                ThreadSafety.EnsureNotUpdateThread();

                try
                {
                    Host.ExitFromRunner();
                    thread.Join();
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

                await CheckForErrors();
            }

            public Task CheckForErrors()
            {
                ThreadSafety.EnsureNotUpdateThread();

                if (Host.ExecutionState == ExecutionState.Stopping)
                    thread.Join();

                if (hostException != null)
                    return Task.FromException(hostException);

                return Task.CompletedTask;
            }

            public async Task RunOnUpdateThread(Func<Task> action)
            {
                await Task.Delay(200).ConfigureAwait(false);

                SynchronizationContext.SetSynchronizationContext(Host.UpdateThread.SynchronizationContext);
                await Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
            }
        }

        private class UpdateThreadTestExecutor(HostContext hostContext) : ITestExecutor
        {
            public async Task ExecuteTest(TestContext context, Func<Task> action) => await hostContext.RunOnUpdateThread(action);
        }
    }
}
