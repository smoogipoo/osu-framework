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
        private TestReceiver receiver;
        private TestReceiver nestedReceiver;

        [SetUp]
        public void Setup()
        {
            receiver = new TestReceiver();
            nestedReceiver = new TestReceiver();
        }

        [Test]
        public void TestSingleAbsoluteSequence([Values] bool recursive)
        {
            using (receiver.BeginAbsoluteSequence(1000, recursive))
                checkDelay(1000, recursive);

            checkDelay(0, recursive);
        }

        [Test]
        public void TestNestedAbsoluteSequence([Values] bool recursive)
        {
            using (receiver.BeginAbsoluteSequence(1000, recursive))
            {
                checkDelay(1000, recursive);

                using (receiver.BeginAbsoluteSequence(500, recursive))
                {
                    checkDelay(500, recursive);

                    using (receiver.BeginAbsoluteSequence(2000, recursive))
                        checkDelay(2000, recursive);

                    checkDelay(500, recursive);
                }

                checkDelay(1000, recursive);
            }

            checkDelay(0, recursive);
        }

        [Test]
        public void TestNestedDoesNotReceiveNonRecursiveSequence()
        {
            using (receiver.BeginAbsoluteSequence(1000, false))
            {
                checkDelay(1000, false);
                checkDelay(0, true);

                using (receiver.BeginAbsoluteSequence(500))
                {
                    checkDelay(500, false);
                    checkDelay(500, true);

                    using (receiver.BeginAbsoluteSequence(2000, false))
                    {
                        checkDelay(2000, false);
                        checkDelay(500, true);
                    }

                    checkDelay(500, false);
                    checkDelay(500, true);
                }

                checkDelay(1000, false);
                checkDelay(0, true);
            }

            checkDelay(0, false);
            checkDelay(0, true);
        }

        private TestReceiver getReceiver(bool nested) => nested ? nestedReceiver : receiver;

        private void checkDelay(double delay, bool nested) => Assert.That(getReceiver(nested).TransformDelay, Is.EqualTo(delay));

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
