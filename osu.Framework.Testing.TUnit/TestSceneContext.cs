// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    internal record TestSceneContext
    {
        public readonly AdhocTestScene.TestSceneHost Host;
        public readonly ITestSceneTestRunner Runner;

        private readonly Game game;
        private readonly Thread thread;

        private Exception? hostException;

        public TestSceneContext(TestScene testScene)
        {
            Host = new AdhocTestScene.TestSceneHost(testScene, $"{testScene.GetType().Name}-{Guid.NewGuid()}");
            Runner = testScene.CreateRunner();

            if (Runner is not Game)
                throw new InvalidCastException($"The test runner must be a {nameof(Game)}.");

            game = (Game)Runner;
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
                await CheckForErrors().ConfigureAwait(false);
                await Task.Delay(10).ConfigureAwait(false);
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

            await CheckForErrors().ConfigureAwait(false);
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
    }
}
