// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a method which adds <see cref="AdhocTestScene"/> steps at the end.
    /// Invoked via <see cref="AdhocTestScene.RunTearDownSteps"/> (which is called from nUnit's [TearDown] or <see cref="AdhocTestBrowser.LoadTest"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class TearDownStepsAttribute : Attribute;
}
