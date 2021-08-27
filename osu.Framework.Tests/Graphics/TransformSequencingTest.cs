// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TransformSequencingTest
    {
        private TestReceiver outer;
        private TestReceiver inner;

        [SetUp]
        public void Setup()
        {
            outer = new TestReceiver();
            inner = new TestReceiver();
        }

        [Test]
        public void TestDefaultZeroDelay()
        {
            checkDelay(outer, 0);
            checkDelay(inner, 0);
        }

        [Test]
        public void TestSingleAbsoluteSequenceAffectsOuter()
        {
            using (outer.BeginAbsoluteSequence(1000, false))
                checkDelay(outer, 1000);

            checkDelay(outer, 0);

            using (outer.BeginAbsoluteSequence(1000, true))
                checkDelay(outer, 1000);
        }

        [Test]
        public void TestSingleAbsoluteSequenceAffectsInner()
        {
            using (outer.BeginAbsoluteSequence(1000, false))
                checkDelay(inner, 0);

            checkDelay(inner, 0);

            using (outer.BeginAbsoluteSequence(1000, true))
                checkDelay(inner, 1000);
        }

        [Test]
        public void TestNestedAbsoluteSequence()
        {
            using (outer.BeginAbsoluteSequence(1000, true))
            {
                checkDelay(outer, 1000);
                checkDelay(inner, 1000);

                using (outer.BeginAbsoluteSequence(500, false))
                {
                    checkDelay(outer, 500);
                    checkDelay(inner, 1000);

                    using (outer.BeginAbsoluteSequence(2000, true))
                    {
                        checkDelay(outer, 2000);
                        checkDelay(inner, 2000);
                    }

                    checkDelay(outer, 500);
                    checkDelay(inner, 1000);
                }

                checkDelay(outer, 1000);
                checkDelay(inner, 1000);
            }

            checkDelay(outer, 0);
            checkDelay(inner, 0);
        }

        private void checkDelay(TestReceiver receiver, double delay) => Assert.That(receiver.TransformDelay, Is.EqualTo(delay));

        private class TestReceiver : Transformable
        {
            public override IFrameBasedClock Clock { get; set; } = new FramedClock(new StopwatchClock());

            public new double TransformDelay => base.TransformDelay;

            internal override void EnsureTransformMutationAllowed()
            {
            }
        }
    }
}
