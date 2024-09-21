// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace osu.Framework.Testing
{
    public static class Assertions
    {
        public static DelayedInvocationAssertionBuilder<TActual, TAnd, TOr> AtSomePoint<TActual, TAnd, TOr>(this InvokableAssertionBuilder<TActual, TAnd, TOr> assertion)
            where TAnd : IAnd<TActual, TAnd, TOr>
            where TOr : IOr<TActual, TAnd, TOr>
            => new DelayedInvocationAssertionBuilder<TActual, TAnd, TOr>(assertion);

        public class DelayedInvocationAssertionBuilder<TActual, TAnd, TOr> :
            AssertionBuilder<TActual, TAnd, TOr>,
            IInvokableAssertionBuilder
            where TAnd : IAnd<TActual, TAnd, TOr>
            where TOr : IOr<TActual, TAnd, TOr>
        {
            private readonly InvokableAssertionBuilder<TActual, TAnd, TOr> builder;
            private readonly TimeSpan timePerAction;

            public DelayedInvocationAssertionBuilder(InvokableAssertionBuilder<TActual, TAnd, TOr> builder)
                : base(builder.AssertionDataDelegate, string.Empty)
            {
                this.builder = builder;

                builder.AppendExpression($"{nameof(AtSomePoint)}()");

                timePerAction = TimeSpan.FromMilliseconds((TestContext.Current?.TestDetails.ClassInstance as TestScene)?.TimePerAction ?? 200);
            }

            public async Task ProcessAssertionsAsync()
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                int timeoutTime = FrameworkEnvironment.NoTestTimeout ? int.MaxValue : 10000;

                while (true)
                {
                    try
                    {
                        await builder.ProcessAssertionsAsync();
                        break;
                    }
                    catch (TUnit.Assertions.Exceptions.AssertionException)
                    {
                        if (stopwatch.ElapsedMilliseconds >= timeoutTime)
                            throw;

                        await Task.Delay(timePerAction);
                    }
                }
            }

            public IAsyncEnumerable<BaseAssertCondition> GetFailures() => builder.GetFailures();

            public TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();

            public string? GetExpression() => builder.GetExpression();
        }
    }
}
