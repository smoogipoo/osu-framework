// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a method which adds <see cref="AdhocTestScene"/> steps.
    /// Invoked via <see cref="AdhocTestScene.RunSetUpSteps"/> (which is called from nUnit's [SetUp] or <see cref="AdhocTestBrowser.LoadTest"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class SetUpStepsAttribute : Attribute;
}
