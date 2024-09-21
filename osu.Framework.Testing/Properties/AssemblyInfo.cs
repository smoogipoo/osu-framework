// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using osu.Framework.Testing;

[assembly: InternalsVisibleTo("osu.Framework.Testing.NUnit")]
[assembly: InternalsVisibleTo("osu.Framework.Testing.TUnit")]
[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: MetadataUpdateHandler(typeof(HotReloadCallbackReceiver))]
