// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL3;
using osuTK;
using osuTK.Graphics;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static SDL.SDL3;

namespace osu.Framework.Graphics.Rendering.SDL3
{
    public unsafe class SDL3Renderer : Renderer
    {
        protected internal override bool VerticalSync { get; set; }
        protected internal override bool AllowTearing { get; set; }
        public override bool IsDepthRangeZeroToOne { get; }
        public override bool IsUvOriginTopLeft { get; }
        public override bool IsClipSpaceYInverted { get; }

        private SDL_GPUDevice* device;
        private SDL_Window* window;

        /// <summary>
        /// The render/compute command buffer for the current frame.
        /// </summary>
        private SDL_GPUCommandBuffer* currentRenderCommandBuffer;

        /// <summary>
        /// The copy command buffer for the current frame.
        /// </summary>
        private SDL_GPUCommandBuffer* currentCopyComandBuffer;

        /// <summary>
        /// The swapchain texture for the current frame.
        /// </summary>
        private SDL_GPUTexture* currentSwapchainTexture;

        /// <summary>
        /// The currently-active render pass.
        /// </summary>
        private SDL_GPURenderPass* currentRenderPass;

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            if (graphicsSurface is not SDL3GraphicsSurface sdl3Surface)
                throw new NotSupportedException($"{nameof(SDL3Renderer)} can only be used with an SDL3 window.");

            window = sdl3Surface.SDLWindowHandle;
            device = SDL_CreateGPUDevice(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV, true, (byte*)null);
            if (device == null)
                throw new InvalidOperationException("Failed to initialise SDL GPU device.");

            if (!SDL_ClaimWindowForGPUDevice(device, window))
                throw new InvalidOperationException("Failed to claim window for SDL GPU device.");
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            currentRenderCommandBuffer = SDL_AcquireGPUCommandBuffer(device);
            if (currentRenderCommandBuffer == null)
                throw new InvalidOperationException("Failed to create the render command buffer.");

            currentCopyComandBuffer = SDL_AcquireGPUCommandBuffer(device);
            if (currentCopyComandBuffer == null)
                throw new InvalidOperationException("Failed to create the copy command buffer.");

            while (true)
            {
                SDL_GPUTexture* tex;
                if (!SDL_WaitAndAcquireGPUSwapchainTexture(currentRenderCommandBuffer, window, &tex, null, null))
                    throw new InvalidOperationException("Failed to retrieve a swapchain texture.");

                if (tex != null)
                {
                    currentSwapchainTexture = tex;
                    break;
                }

                // Swapchain texture can be null while the window is minimized.
                // Todo: Instead of this, we should early exit out of DrawFrame().
                Thread.Sleep(10);
            }

            base.BeginFrame(windowSize);
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            endRenderPass();

            SDL_SubmitGPUCommandBuffer(currentCopyComandBuffer);
            SDL_SubmitGPUCommandBuffer(currentRenderCommandBuffer);

            currentRenderCommandBuffer = null;
            currentCopyComandBuffer = null;
            currentSwapchainTexture = null;
            Debug.Assert(currentRenderPass == null);
        }

        protected internal override void SwapBuffers()
        {
        }

        protected internal override void WaitUntilIdle()
        {
        }

        protected internal override void WaitUntilNextFrameReady()
        {
        }

        protected internal override void MakeCurrent()
        {
        }

        protected internal override void ClearCurrent()
        {
        }

        protected override void ClearImplementation(ClearInfo clearInfo)
        {
            enableRenderPass(clearInfo);
        }

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
        {
        }

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
        {
        }

        protected override void SetViewportImplementation(RectangleI viewport)
        {
        }

        protected override void SetScissorImplementation(RectangleI scissor)
        {
        }

        protected override void SetScissorStateImplementation(bool enabled)
        {
        }

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
        {
        }

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
        {
        }

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            return true;
        }

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
        {
        }

        protected override void DeleteFrameBufferImplementation(IFrameBuffer frameBuffer)
        {
        }

        public override void DrawVerticesImplementation(PrimitiveTopology topology, int vertexStart, int verticesCount)
        {
        }

        protected override void SetShaderImplementation(IShader shader)
        {
        }

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
        }

        protected override void SetUniformBufferImplementation(string blockName, IUniformBuffer buffer)
        {
        }

        private void enableRenderPass(ClearInfo? clearInfo)
        {
            if (currentRenderPass != null)
                return;

            SDL_GPUColorTargetInfo colourTargetInfo = new SDL_GPUColorTargetInfo
            {
                texture = currentSwapchainTexture,
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            };

            if (clearInfo is ClearInfo clear)
            {
                colourTargetInfo.clear_color = new SDL_FColor { r = clear.Colour.R, g = clear.Colour.G, b = clear.Colour.B, a = clear.Colour.A };
                colourTargetInfo.load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR;
            }

            currentRenderPass = SDL_BeginGPURenderPass(currentRenderCommandBuffer, &colourTargetInfo, 1, null);
        }

        private void endRenderPass()
        {
            if (currentRenderPass == null)
                return;

            SDL_EndGPURenderPass(currentRenderPass);
            currentRenderPass = null;
        }

        protected internal override Image<Rgba32> TakeScreenshot()
        {
            throw new NotImplementedException();
        }

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
        {
            return new DummyFrameBuffer(this);
        }

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
        {
            return new DummyVertexBatch<TVertex>();
        }

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
        {
            return new DummyVertexBatch<TVertex>();
        }

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
        {
            return new DummyUniformBuffer<TData>();
        }

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
        {
            return new DummyShaderStorageBufferObject<TData>(uboSize);
        }

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
        {
            return new DummyNativeTexture(this, width, height);
        }

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
        {
            return new DummyNativeTexture(this, width, height);
        }

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
        {
            return new DummyShaderPart();
        }

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
        {
            return new DummyShader(this);
        }
    }
}
