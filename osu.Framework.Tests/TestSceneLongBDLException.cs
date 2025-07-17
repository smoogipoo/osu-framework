// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests
{
    public class TestSceneLongBdlException : FrameworkTestScene
    {
        [Test]
        public void TestThrowFromUpdate()
        {
            int currentDepth = 0;
            AddStep("add thrower", () => Child = new Thrower(5, ref currentDepth));
        }

        private class Thrower : CompositeDrawable
        {
            private readonly bool shouldThrow;

            public Thrower(int maxDepth, ref int currentDepth)
            {
                if (++currentDepth == maxDepth)
                    shouldThrow = true;
                else
                    InternalChild = new Thrower(maxDepth, ref currentDepth);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (shouldThrow)
                    throw new InvalidOperationException("whoop!");
            }
        }
    }
}
