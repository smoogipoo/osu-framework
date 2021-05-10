// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkAutoSize : GameBenchmark
    {
        private TestGame game;

        [Test]
        [Benchmark]
        public void TestInvalidateTopDown()
        {
            game.Schedule(() => game.Size = new Vector2(RNG.NextSingle()));
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TestInvalidateBottomUp()
        {
            game.Schedule(() => game.MostNestedDrawable.Size = new Vector2(RNG.NextSingle()));
            RunSingleFrame();
        }

        protected override Game CreateGame() => game = new TestGame();

        private class TestGame : Game
        {
            private const int levels = 100;

            public Drawable MostNestedDrawable { get; private set; }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                int currentLevel = 1;
                Container container = createContainer();
                Add(container);

                while (currentLevel++ < levels)
                    container.Child = container = createContainer();

                container.Add(MostNestedDrawable = new Box { Size = new Vector2(100) });
            }

            private Container createContainer() => new Container
            {
                AutoSizeAxes = Axes.Both,
                ChildrenEnumerable = Enumerable.Range(0, 5).Select(_ => new Box { RelativeSizeAxes = Axes.Both })
            };

            public new void Schedule(Action action) => base.Schedule(action);
        }
    }
}
