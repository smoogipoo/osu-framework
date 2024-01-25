// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Platform;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridRenderer : Renderer, IVeldridRenderer
    {
        public GraphicsSurfaceType SurfaceType => veldridDevice.SurfaceType;

        protected internal override bool VerticalSync
        {
            get => veldridDevice.VerticalSync;
            set => veldridDevice.VerticalSync = value;
        }

        protected internal override bool AllowTearing
        {
            get => veldridDevice.AllowTearing;
            set => veldridDevice.AllowTearing = value;
        }

        public override bool IsDepthRangeZeroToOne => veldridDevice.IsDepthRangeZeroToOne;
        public override bool IsUvOriginTopLeft => veldridDevice.IsUvOriginTopLeft;
        public override bool IsClipSpaceYInverted => veldridDevice.IsClipSpaceYInverted;

        public GraphicsDevice Device => veldridDevice.Device;

        public ResourceFactory Factory => veldridDevice.Factory;

        private bool beganTextureUpdatePipeline;

        private VeldridIndexBuffer? linearIndexBuffer;
        private VeldridIndexBuffer? quadIndexBuffer;

        private readonly HashSet<IVeldridUniformBuffer> uniformBufferResetList = new HashSet<IVeldridUniformBuffer>();

        private VeldridDevice veldridDevice = null!;
        private SimplePipeline bufferUpdatePipeline = null!;
        private SimplePipeline textureUpdatePipeline = null!;

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            veldridDevice = new VeldridDevice(graphicsSurface);
            bufferUpdatePipeline = new SimplePipeline(veldridDevice);
            textureUpdatePipeline = new SimplePipeline(veldridDevice);

            MaxTextureSize = veldridDevice.MaxTextureSize;
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            foreach (var ubo in uniformBufferResetList)
                ubo.ResetCounters();
            uniformBufferResetList.Clear();

            veldridDevice.BeginFrame(new Vector2I((int)windowSize.X, (int)windowSize.Y));
            bufferUpdatePipeline.Begin();

            base.BeginFrame(windowSize);
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            flushTextureUploadCommands();

            bufferUpdatePipeline.End();
            veldridDevice.FinishFrame();
        }

        protected internal override void SwapBuffers()
            => veldridDevice.SwapBuffers();

        protected internal override void WaitUntilIdle()
            => veldridDevice.WaitUntilIdle();

        protected internal override void WaitUntilNextFrameReady()
            => veldridDevice.WaitUntilNextFrameReady();

        protected internal override void MakeCurrent()
            => veldridDevice.MakeCurrent();

        protected internal override void ClearCurrent()
            => veldridDevice.ClearCurrent();

        protected override void ClearImplementation(ClearInfo clearInfo)
            => veldridDevice.Graphics.Clear(clearInfo);

        protected override void SetScissorStateImplementation(bool enabled)
            => veldridDevice.Graphics.SetScissorState(enabled);

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            if (texture is not VeldridTexture veldridTexture)
                return false;

            veldridDevice.Graphics.AttachTexture(unit, veldridTexture);
            return true;
        }

        protected override void SetShaderImplementation(IShader shader)
            => veldridDevice.Graphics.SetShader((VeldridShader)shader);

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
            => veldridDevice.Graphics.SetBlend(blendingParameters);

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
            => veldridDevice.Graphics.SetBlendMask(blendingMask);

        protected override void SetViewportImplementation(RectangleI viewport)
            => veldridDevice.Graphics.SetViewport(viewport);

        protected override void SetScissorImplementation(RectangleI scissor)
            => veldridDevice.Graphics.SetScissor(scissor);

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
            => veldridDevice.Graphics.SetDepthInfo(depthInfo);

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
            => veldridDevice.Graphics.SetStencilInfo(stencilInfo);

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
            => veldridDevice.Graphics.SetFrameBuffer((VeldridFrameBuffer?)frameBuffer);

        public void BindVertexBuffer(IVeldridVertexBuffer buffer, VertexLayoutDescription layout)
            => veldridDevice.Graphics.SetVertexBuffer(buffer.Buffer, layout);

        public void BindIndexBuffer(VeldridIndexLayout layout, int verticesCount)
        {
            ref var indexBuffer = ref layout == VeldridIndexLayout.Quad
                ? ref quadIndexBuffer
                : ref linearIndexBuffer;

            if (indexBuffer == null || indexBuffer.VertexCapacity < verticesCount)
            {
                indexBuffer?.Dispose();
                indexBuffer = new VeldridIndexBuffer(this, layout, verticesCount);
            }

            veldridDevice.Graphics.SetIndexBuffer(indexBuffer);
        }

        public override void DrawVerticesImplementation(Rendering.PrimitiveTopology type, int vertexStart, int verticesCount)
        {
            // normally we would flush/submit all texture upload commands at the end of the frame, since no actual rendering by the GPU will happen until then,
            // but turns out on macOS with non-apple GPU, this results in rendering corruption.
            // flushing the texture upload commands here before a draw call fixes the corruption, and there's no explanation as to why that's the case,
            // but there is nothing to be lost in flushing here except for a frame that contains many sprites with Texture.BypassTextureUploadQueue = true.
            // until that appears to be problem, let's just flush here.
            flushTextureUploadCommands();

            veldridDevice.Graphics.DrawVertices(type.ToPrimitiveTopology(), vertexStart, verticesCount);
        }

        private void ensureTextureUploadCommandsBegan()
        {
            if (beganTextureUpdatePipeline)
                return;

            textureUpdatePipeline.Begin();
            beganTextureUpdatePipeline = true;
        }

        private void flushTextureUploadCommands()
        {
            if (!beganTextureUpdatePipeline)
                return;

            textureUpdatePipeline.End();
            beganTextureUpdatePipeline = false;
        }

        protected internal override Image<Rgba32> TakeScreenshot()
            => veldridDevice.TakeScreenshot();

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => new VeldridShaderPart(this, rawData, partType, store);

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
            => new VeldridShader(this, name, parts.Cast<VeldridShaderPart>().ToArray(), compilationStore);

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new VeldridFrameBuffer(this, renderBufferFormats?.ToPixelFormats(), filteringMode.ToSamplerFilter());

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, Rendering.PrimitiveTopology primitiveType)
            // maxBuffers is ignored because batches are not allowed to wrap around in Veldrid.
            => new VeldridLinearBatch<TVertex>(this, size, primitiveType);

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
        {
            // maxBuffers is ignored because batches are not allowed to wrap around in Veldrid.
            return new VeldridQuadBatch<TVertex>(this, size);
        }

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new VeldridUniformBuffer<TData>(this);

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            => new VeldridShaderStorageBufferObject<TData>(this, uboSize, ssboSize);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
            => new VeldridTexture(this, width, height, manualMipmaps, filteringMode.ToSamplerFilter(), initialisationColour);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
            => new VeldridVideoTexture(this, width, height);

        internal IStagingBuffer<T> CreateStagingBuffer<T>(uint count)
            where T : unmanaged
        {
            switch (FrameworkEnvironment.StagingBufferType)
            {
                case 0:
                    return new ManagedStagingBuffer<T>(this, count);

                case 1:
                    return new PersistentStagingBuffer<T>(this, count);

                case 2:
                    return new DeferredStagingBuffer<T>(this, count);

                default:
                    switch (veldridDevice.Device.BackendType)
                    {
                        case GraphicsBackend.Direct3D11:
                        case GraphicsBackend.Vulkan:
                            return new PersistentStagingBuffer<T>(this, count);

                        default:
                        // Metal uses a more optimal path that elides a Blit Command Encoder.
                        case GraphicsBackend.Metal:
                        // OpenGL backends need additional work to support coherency and persistently mapped buffers.
                        case GraphicsBackend.OpenGL:
                        case GraphicsBackend.OpenGLES:
                            return new ManagedStagingBuffer<T>(this, count);
                    }
            }
        }

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
        }

        #region IVeldridRenderer

        bool IVeldridRenderer.UseStructuredBuffers
            => veldridDevice.UseStructuredBuffers;

        CommandList IVeldridRenderer.BufferUpdateCommands
            => bufferUpdatePipeline.Commands;

        void IVeldridRenderer.UpdateTexture<T>(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
        {
            ensureTextureUploadCommandsBegan();
            textureUpdatePipeline.UpdateTexture(texture, x, y, width, height, level, data);
        }

        void IVeldridRenderer.UpdateTexture(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
            => bufferUpdatePipeline.UpdateTexture(texture, x, y, width, height, level, data, rowLengthInBytes);

        void IVeldridRenderer.EnqueueTextureUpload(VeldridTexture texture)
            => EnqueueTextureUpload(texture);

        void IVeldridRenderer.GenerateMipmaps(VeldridTexture texture)
            => veldridDevice.Graphics.Commands.GenerateMipmaps(texture.GetResourceList().Single().Texture);

        void IVeldridRenderer.BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer)
            => veldridDevice.Graphics.AttachUniformBuffer(blockName, veldridBuffer);

        void IVeldridRenderer.BindFrameBuffer(VeldridFrameBuffer frameBuffer)
            => BindFrameBuffer(frameBuffer);

        void IVeldridRenderer.UnbindFrameBuffer(VeldridFrameBuffer frameBuffer)
            => UnbindFrameBuffer(frameBuffer);

        bool IVeldridRenderer.IsFrameBufferBound(VeldridFrameBuffer frameBuffer)
            => FrameBuffer == frameBuffer;

        void IVeldridRenderer.BindShader(VeldridShader shader)
            => base.BindShader(shader);

        void IVeldridRenderer.UnbindShader(VeldridShader shader)
            => base.UnbindShader(shader);

        void IVeldridRenderer.RegisterUniformBufferForReset(IVeldridUniformBuffer buffer)
            => uniformBufferResetList.Add(buffer);

        Texture IVeldridRenderer.CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS, WrapMode wrapModeT)
            => CreateTexture(nativeTexture, wrapModeS, wrapModeT);

        #endregion
    }
}
