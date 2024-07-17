// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Development;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Framework.Utils;

namespace osu.Framework.Testing
{
    public class HeadlessThreadRunner : ThreadRunner
    {
        public HeadlessThreadRunner(InputThread mainThread)
            : base(mainThread)
        {
        }

        public override void RunMainLoop()
        {
            // Run true single-threaded mode.
            if (ThreadSafety.ExecutionMode == ExecutionMode.SingleThread)
            {
                base.RunMainLoop();
                return;
            }

            // Run a simulated multi-threaded mode.
            // This is because the CI test runners are usually CPU-limited and may be preferring one thread (audio) over another (update).

            ThreadSafety.ExecutionMode = ExecutionMode.SingleThread;

            for (int i = 0; i < Threads.Count; i++)
            {
                int from;
                int to;

                do
                {
                    from = RNG.Next(Threads.Count);
                    to = RNG.Next(Threads.Count);
                } while (from == to);

                (Threads[from], Threads[to]) = (Threads[to], Threads[from]);
            }

            base.RunMainLoop();

            ThreadSafety.ExecutionMode = ExecutionMode.MultiThreaded;
        }
    }
}
