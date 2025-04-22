// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Apple.Native
{
    public readonly struct NSError
    {
        internal IntPtr Handle { get; }

        internal NSError(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
