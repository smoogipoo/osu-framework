// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Graphics._3D
{
    public partial class Model : Transformable, IDisposable
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public DrawInfo DrawInfo => throw new NotImplementedException();

        public DrawColourInfo DrawColourInfo => throw new NotImplementedException();

        public World Parent
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsPresent => throw new NotImplementedException();

        public override IFrameBasedClock Clock
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Vector3 ToSpaceOfOtherModel(Vector3 input, Model other) => throw new NotImplementedException();

        public Vector2 ToSpaceOfOtherDrawable(Vector3 input, IDrawable other) => throw new NotImplementedException();

        public Vector3 ToLocalSpace(Vector3 screenSpacePos) => throw new NotImplementedException();

        public Vector3 ToLocalSpace(Vector2 screenSpacePos) => throw new NotImplementedException();

        public BlendingParameters Blending => throw new NotImplementedException();

        public bool IsHovered => throw new NotImplementedException();

        public bool IsDragged => throw new NotImplementedException();

        public float Alpha => throw new NotImplementedException();

        public void Show() => throw new NotImplementedException();

        public void Hide() => throw new NotImplementedException();

        public long InvalidationID { get; private set; } = 1;

        internal ulong ChildID { get; set; }

        internal override void EnsureTransformMutationAllowed() => throw new NotImplementedException();

        public void Dispose() => throw new NotImplementedException();

        protected virtual void Dispose(bool disposing) => throw new NotImplementedException();
    }
}
