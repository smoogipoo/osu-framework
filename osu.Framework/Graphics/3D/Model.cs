// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Graphics._3D
{
    public partial class Model : Transformable, IDisposable, IDrawable
    {
        public Vector3 DrawSize => throw new NotImplementedException();

        public BoxF DrawRectangle => throw new NotImplementedException();

        Vector2 IDrawable.DrawSize => DrawSize.Xy;

        RectangleF IDrawable.DrawRectangle => new RectangleF(DrawRectangle.Location.Xy, DrawRectangle.Size.Xy);

        DrawInfo IDrawable.DrawInfo => drawInfo;

        DrawColourInfo IDrawable.DrawColourInfo => drawColourInfo;

        Quad IDrawable.ScreenSpaceDrawQuad => screenSpaceDrawQuad;

        CompositeDrawable? IDrawable.Parent => parent;

        public bool IsPresent => throw new NotImplementedException();

        public override IFrameBasedClock Clock
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        Vector2 IDrawable.ToSpaceOfOtherDrawable(Vector2 input, IDrawable other) => throw new NotImplementedException();

        Vector2 IDrawable.ToLocalSpace(Vector2 screenSpacePos) => throw new NotImplementedException();

        public BlendingParameters Blending => throw new NotImplementedException();

        public bool IsHovered => throw new NotImplementedException();

        public bool IsDragged => throw new NotImplementedException();

        public float Alpha => throw new NotImplementedException();

        void IDrawable.Show() => throw new NotImplementedException();

        void IDrawable.Hide() => throw new NotImplementedException();

        public long InvalidationID { get; private set; } = 1;

        internal override void EnsureTransformMutationAllowed()
        {
        }

        public void Dispose()
        {
        }
    }
}
