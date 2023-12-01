// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct SetUniformBufferDataEvent<TData>(IUniformBuffer<TData> Buffer, TData Data) : IEvent
        where TData : unmanaged, IEquatable<TData>;
}
