// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: InternalsVisibleTo("osu.Framework.Android")]
[assembly: InternalsVisibleTo("osu.Framework.Benchmarks")]
[assembly: InternalsVisibleTo("osu.Framework.iOS")]
[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.TUnit")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.iOS")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Android")]
[assembly: InternalsVisibleTo("osu.Framework.Testing")]
[assembly: InternalsVisibleTo("osu.Framework.Testing.NUnit")]
[assembly: InternalsVisibleTo("osu.Framework.Testing.TUnit")]
