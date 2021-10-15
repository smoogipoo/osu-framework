// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkPathApproximator
    {
        [Params(1, 10, 100, 1000)]
        public int NumControlPoints { get; set; }

        private Vector2[] controlPoints;

        [GlobalSetup]
        public void GlobalSetup()
        {
            controlPoints = new Vector2[NumControlPoints];
            for (int i = 0; i < NumControlPoints; i++)
                controlPoints[i] = new Vector2(RNG.Next(0, 500), RNG.Next(0, 500));
        }

        [Benchmark]
        public void Bezier() => PathApproximator.ApproximateBezier(controlPoints);
    }
}
