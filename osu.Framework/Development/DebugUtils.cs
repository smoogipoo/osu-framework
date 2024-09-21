// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        public static bool IsTestRunning { get; internal set; }

        public static bool IsDebugBuild => is_debug_build.Value;

        private static readonly Lazy<bool> is_debug_build = new Lazy<bool>(() =>
            isDebugAssembly(typeof(DebugUtils).Assembly) || isDebugAssembly(RuntimeInfo.EntryAssembly)
        );

        /// <summary>
        /// Whether the framework is currently logging performance issues.
        /// This should be used only when a configuration is not available via DI or otherwise (ie. in a static context).
        /// </summary>
        public static bool LogPerformanceIssues { get; internal set; }

        // https://stackoverflow.com/a/2186634
        private static bool isDebugAssembly(Assembly? assembly) => assembly?.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled) ?? false;
    }
}
