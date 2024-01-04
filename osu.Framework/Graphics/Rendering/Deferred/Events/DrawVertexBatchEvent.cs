// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct DrawVertexBatchEvent<TVertex>(DeferredVertexBatch<TVertex> VertexBatch) : IEvent
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        public void Run(DeferredRenderer current, IRenderer target) => VertexBatch.Resource.Draw();
    }
}
