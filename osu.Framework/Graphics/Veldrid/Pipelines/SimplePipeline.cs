// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    /// <summary>
    /// A non-graphical pipeline that handles basic tasks like uploading textures.
    /// </summary>
    internal class SimplePipeline
    {
        public GraphicsDevice Device => device.Device;

        public ResourceFactory Factory => device.Factory;

        public readonly CommandList Commands;

        private readonly VeldridDevice device;

        public SimplePipeline(VeldridDevice device)
        {
            this.device = device;
            Commands = device.Factory.CreateCommandList();
        }

        /// <summary>
        /// Begins the pipeline.
        /// </summary>
        public virtual void Begin()
        {
            Commands.Begin();
        }

        /// <summary>
        /// Finishes the pipeline and submits it for processing by the device.
        /// </summary>
        public virtual void End(Fence? fence = null)
        {
            Commands.End();
            device.Device.SubmitCommands(Commands, fence);
        }

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        public void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            // This code is doing the same as the simpler approach of:
            //
            // Device.UpdateTexture(texture, data, (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            //
            // Except we are using a staging texture pool to avoid the alloc overhead of each staging texture.
            var staging = device.StagingTexturePool.Get(width, height, texture.Format);
            device.Device.UpdateTexture(staging, data, 0, 0, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            Commands.CopyTexture(staging, 0, 0, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <param name="rowLengthInBytes">The number of bytes per row of the image to read from <paramref name="data"/>.</param>
        public void UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
        {
            var staging = device.StagingTexturePool.Get(width, height, texture.Format);

            unsafe
            {
                MappedResource mappedData = Device.Map(staging, MapMode.Write);

                try
                {
                    void* srcPtr = data.ToPointer();
                    void* dstPtr = mappedData.Data.ToPointer();

                    for (int i = 0; i < height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(dstPtr, srcPtr, mappedData.RowPitch);

                        srcPtr = Unsafe.Add<byte>(srcPtr, rowLengthInBytes);
                        dstPtr = Unsafe.Add<byte>(dstPtr, (int)mappedData.RowPitch);
                    }
                }
                finally
                {
                    Device.Unmap(staging);
                }
            }

            Commands.CopyTexture(
                staging, 0, 0, 0, 0, 0,
                texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }
    }
}
