// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Testing
{
    internal static class Module
    {
        [ModuleInitializer]
        public static void Init()
        {
            DebugUtils.IsTestRunning = isNUnitRunning();
            if (DebugUtils.IsTestRunning)
                RuntimeInfo.EntryAssembly = getNUnitAssembly();
        }

        private static bool isNUnitRunning()
        {
#pragma warning disable RS0030
            var entry = Assembly.GetEntryAssembly();
#pragma warning restore RS0030

            string? assemblyName = entry?.GetName().Name;

            // when running under nunit + netcore, entry assembly becomes nunit itself (testhost, Version=15.0.0.0), which isn't what we want.
            // when running under nunit + Rider > 2020.2 EAP6, entry assembly becomes ReSharperTestRunner[32|64], which isn't what we want.
            bool entryIsKnownTestAssembly = entry != null && (assemblyName!.Contains("testhost") || assemblyName.Contains("ReSharperTestRunner"));

            // null assembly can indicate nunit, but it can also indicate native code (e.g. android).
            // to distinguish nunit runs from android launches, check the class name of the current test.
            // if no actual test is running, nunit will make up an ad-hoc test context, which we can match on
            // to eliminate such false positives.
            bool nullEntryWithActualTestContext = entry == null && TestContext.CurrentContext.Test.ClassName != typeof(TestExecutionContext.AdhocContext).FullName;

            return entryIsKnownTestAssembly || nullEntryWithActualTestContext;
        }

        private static Assembly getNUnitAssembly()
        {
            Debug.Assert(DebugUtils.IsTestRunning);

            string testName = TestContext.CurrentContext.Test.ClassName.AsNonNull();
            return AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetType(testName) != null);
        }
    }
}
