// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Veldrid.Pipelines
{
    internal class VeldridDevicePipeline : IVeldridPipeline
    {
        private readonly GraphicsDevice device;
        private Vector2I currentWindowSize;

        public VeldridDevicePipeline(IVeldridDevice device)
        {
            this.device = device.Device;
        }

        void IVeldridPipeline.Begin() => throw new InvalidOperationException();

        public void Begin(Vector2I windowSize)
        {
            if (windowSize != currentWindowSize)
            {
                device.ResizeMainWindow((uint)windowSize.X, (uint)windowSize.Y);
                currentWindowSize = windowSize;
            }
        }

        public void End()
        {
        }
    }
}
