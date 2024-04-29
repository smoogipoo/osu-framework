// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Rendering.Direct3D12
{
    /// <summary>
    /// An <see cref="IRenderer"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    public sealed class Direct3D12Renderer : IRenderer
    {
        public int MaxTextureSize => int.MaxValue;
        public int MaxTexturesUploadedPerFrame { get; set; } = int.MaxValue;
        public int MaxPixelsUploadedPerFrame { get; set; } = int.MaxValue;

        public bool IsDepthRangeZeroToOne => true;
        public bool IsUvOriginTopLeft => true;
        public bool IsClipSpaceYInverted => true;
        public ref readonly MaskingInfo CurrentMaskingInfo => ref maskingInfo;
        private readonly MaskingInfo maskingInfo;

        public RectangleI Viewport => RectangleI.Empty;
        public RectangleF Ortho => RectangleF.Empty;
        public RectangleI Scissor => RectangleI.Empty;
        public Vector2I ScissorOffset => Vector2I.Zero;
        public Matrix4 ProjectionMatrix => Matrix4.Identity;
        public DepthInfo CurrentDepthInfo => DepthInfo.Default;
        public StencilInfo CurrentStencilInfo => StencilInfo.Default;
        public WrapMode CurrentWrapModeS => WrapMode.None;
        public WrapMode CurrentWrapModeT => WrapMode.None;
        public bool IsMaskingActive => false;
        public bool UsingBackbuffer => false;
        public Texture WhitePixel { get; }
        DepthValue IRenderer.BackbufferDepth { get; } = new DepthValue();

        public bool IsInitialised { get; private set; }

        public Direct3D12Renderer()
        {
            maskingInfo = default;
            WhitePixel = new TextureWhitePixel(new Texture(new DummyNativeTexture(this), WrapMode.None, WrapMode.None));
        }

        public ulong FrameIndex { get; private set; }

        bool IRenderer.VerticalSync { get; set; } = true;

        bool IRenderer.AllowTearing { get; set; }

        Storage? IRenderer.CacheStorage { set { } }

        private const int num_buffers = 3;

        private ID3D12Device2 device;
        private IDXGIFactory4 dxgiFactory;

        private ID3D12CommandQueue commandQueue;
        private IDXGISwapChain4 swapChain;

        private readonly ulong[] backBufferFrameIndex = new ulong[num_buffers];
        private ID3D12DescriptorHeap backBufferDescriptorHeap;
        private readonly ID3D12Resource[] backBuffers = new ID3D12Resource[num_buffers];

        private readonly ID3D12CommandAllocator[] commandAllocators = new ID3D12CommandAllocator[num_buffers];
        private ID3D12GraphicsCommandList commandList;

        private ID3D12Fence frameFence;
        private Vector2 currentWindowSize;

        void IRenderer.Initialise(IGraphicsSurface graphicsSurface)
        {
            // Create the device.

            if (DebugUtils.IsDebugBuild)
            {
                D3D12.D3D12GetDebugInterface(out ID3D12Debug? debug);
                debug?.EnableDebugLayer();
            }

            device = D3D12.D3D12CreateDevice<ID3D12Device2>(null, FeatureLevel.Level_12_0);

            if (DebugUtils.IsDebugBuild)
            {
                ID3D12InfoQueue1? queue = device.QueryInterfaceOrNull<ID3D12InfoQueue1>();
                queue?.RegisterMessageCallback(debugMessageCallback, MessageCallbackFlags.None);
            }

            // Create the DXGI factory.

            dxgiFactory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(DebugUtils.IsDebugBuild);

            // Create the command queue.
            // This is the global queue for the device that, importantly, holds command lists (the actual render commands) and fences.

            commandQueue = device.CreateCommandQueue<ID3D12CommandQueue>(new CommandQueueDescription
            {
                Type = CommandListType.Direct
            });

            // Create the swapchain.

            swapChain = dxgiFactory.CreateSwapChainForHwnd(
                commandQueue,
                graphicsSurface.WindowHandle,
                new SwapChainDescription1
                {
                    BufferCount = num_buffers,
                    Format = Format.R8G8B8A8_UNorm,
                    BufferUsage = Usage.RenderTargetOutput,
                    SwapEffect = SwapEffect.FlipDiscard,
                    SampleDescription = new SampleDescription(1, 0)
                },
                new SwapChainFullscreenDescription
                {
                    Windowed = true
                }
            ).QueryInterface<IDXGISwapChain4>();

            // Ignore alt-enter-to-fullscreen because we handle this state transition ourselves.

            dxgiFactory.MakeWindowAssociation(graphicsSurface.WindowHandle, WindowAssociationFlags.IgnoreAltEnter);

            // Create the descriptor heap.
            // This holds the handles to textures and buffers for use in the pipeline.

            backBufferDescriptorHeap = device.CreateDescriptorHeap<ID3D12DescriptorHeap>(new DescriptorHeapDescription
            {
                DescriptorCount = num_buffers,
                Type = DescriptorHeapType.RenderTargetView
            });

            // Create the backbuffers.

            initialiseBackBuffers();

            // Create the command allocators, one per frame.
            // These are what we'll use to write to the command list.
            // Somewhat counter-intuitively, these cannot be reused until their respective command list is finished executing.

            for (int i = 0; i < num_buffers; i++)
                commandAllocators[i] = device.CreateCommandAllocator<ID3D12CommandAllocator>(CommandListType.Direct);

            // Create the command list.
            // These can be re-used immediately, provided that they are reset to a command allocator that's not in-flight.
            // Again, this is somewhat counter-intuitive - think of command allocators as a deferred storage of the commands to execute,
            // and the command list as an entry point for us to enqueue the commands with.

            commandList = device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Direct, commandAllocators[0]);
            commandList.Close();

            // Create the frame fence.

            frameFence = device.CreateFence<ID3D12Fence>();

            IsInitialised = true;
        }

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
            if (currentWindowSize != windowSize)
            {
                // Finish up any in-flight frames.

                waitForFrame(FrameIndex);
                for (int i = 0; i < num_buffers; i++)
                    backBufferFrameIndex[i] = FrameIndex;

                // Resize the swapchain.

                SwapChainDescription1 desc = swapChain.Description1;
                swapChain.ResizeBuffers(num_buffers, (int)windowSize.X, (int)windowSize.Y, desc.Format, desc.Flags);

                currentWindowSize = windowSize;
            }

            FrameIndex++;

            // By this point, we assume that WaitUntilNextFrameReady() has been called, ensuring that we can write to the command allocator/backbuffer.

            ID3D12CommandAllocator allocator = commandAllocators[swapChain.CurrentBackBufferIndex];
            ID3D12Resource backBuffer = backBuffers[swapChain.CurrentBackBufferIndex];

            allocator.Reset();

            commandList.Reset(allocator);
            commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(backBuffer, ResourceStates.Present, ResourceStates.RenderTarget));

            CpuDescriptorHandle backBufferHandle = new CpuDescriptorHandle(
                backBufferDescriptorHeap.GetCPUDescriptorHandleForHeapStart(),
                swapChain.CurrentBackBufferIndex,
                device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView));

            commandList.ClearRenderTargetView(backBufferHandle, new Vortice.Mathematics.Color4(1.0f));
        }

        void IRenderer.FinishFrame()
        {
            ID3D12Resource backBuffer = backBuffers[swapChain.CurrentBackBufferIndex];

            // Enqueue the command list.

            device.CreateCommittedResource<ID3D12Resource>(HeapProperties.UploadHeapProperties, )

            commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(backBuffer, ResourceStates.RenderTarget, ResourceStates.Present));
            commandList.Close();
            commandQueue.ExecuteCommandList(commandList);

            // Signal the fence with the current frame index after the commands complete.

            backBufferFrameIndex[swapChain.CurrentBackBufferIndex] = FrameIndex;
            commandQueue.Signal(frameFence, FrameIndex);

            // And finally present the frame.

            swapChain.Present(0, PresentFlags.None);
        }

        private void initialiseBackBuffers()
        {
            // Create the backbuffers, one per frame we want.
            // These are the same resources like any other, and so must go on the descriptor heap.

            CpuDescriptorHandle backBufferHandle = backBufferDescriptorHeap.GetCPUDescriptorHandleForHeapStart();

            for (int i = 0; i < num_buffers; i++)
            {
                backBuffers[i] = swapChain.GetBuffer<ID3D12Resource>(i);
                device.CreateRenderTargetView(backBuffers[i], null, backBufferHandle);
                backBufferHandle = backBufferHandle.Offset(device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView));
            }
        }

        private void debugMessageCallback(MessageCategory category, MessageSeverity severity, MessageId id, string description)
        {
            Logger.Log($"[D3D12] [{category}] [{severity}]: {description}");
        }

        void IRenderer.FlushCurrentBatch(FlushBatchSource? source)
        {
        }

        void IRenderer.SwapBuffers()
        {
        }

        void IRenderer.WaitUntilIdle()
        {
        }

        void IRenderer.WaitUntilNextFrameReady() => waitForFrame(backBufferFrameIndex[swapChain.CurrentBackBufferIndex]);

        private void waitForFrame(ulong frameIndex)
        {
            if (frameFence.CompletedValue >= frameIndex)
                return;

            using ManualResetEventSlim frameEvent = new ManualResetEventSlim();
            frameFence.SetEventOnCompletion(frameIndex, frameEvent.WaitHandle);
            frameEvent.Wait(TimeSpan.FromSeconds(1));
        }

        void IRenderer.MakeCurrent()
        {
        }

        void IRenderer.ClearCurrent()
        {
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
            => true;

        public void UseProgram(IShader? shader)
        {
        }

        public void Clear(ClearInfo clearInfo)
        {
        }

        public void PushScissorState(bool enabled)
        {
        }

        public void PopScissorState()
        {
        }

        public void SetBlend(BlendingParameters blendingParameters)
        {
        }

        public void SetBlendMask(BlendingMask blendingMask)
        {
        }

        public void PushViewport(RectangleI viewport)
        {
        }

        public void PopViewport()
        {
        }

        public void PushScissor(RectangleI scissor)
        {
        }

        public void PopScissor()
        {
        }

        public void PushScissorOffset(Vector2I offset)
        {
        }

        public void PopScissorOffset()
        {
        }

        public void PushProjectionMatrix(Matrix4 matrix)
        {
        }

        public void PopProjectionMatrix()
        {
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
        }

        public void PopMaskingInfo()
        {
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
        }

        public void PopDepthInfo()
        {
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
        }

        public void PopStencilInfo()
        {
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation) => operation.RunTask();

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target) => disposalAction(target);

        Image<Rgba32> IRenderer.TakeScreenshot() => new Image<Rgba32>(1366, 768);

        IShaderPart IRenderer.CreateShaderPart(IShaderStore manager, string name, byte[]? rawData, ShaderPartType partType)
            => new DummyShaderPart();

        IShader IRenderer.CreateShader(string name, IShaderPart[] parts)
            => new DummyShader(this);

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer(this);

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Color4? initialisationColour = null)
            => new Texture(new DummyNativeTexture(this) { Width = width, Height = height }, wrapModeS, wrapModeT);

        public Texture CreateVideoTexture(int width, int height)
            => CreateTexture(width, height);

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public IUniformBuffer<TData> CreateUniformBuffer<TData>() where TData : unmanaged, IEquatable<TData>
            => new DummyUniformBuffer<TData>();

        public IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize) where TData : unmanaged, IEquatable<TData>
            => new DummyShaderStorageBufferObject<TData>(ssboSize);

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
        }

        IVertexBatch<TexturedVertex2D> IRenderer.DefaultQuadBatch => new DummyVertexBatch<TexturedVertex2D>();

        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch)
        {
        }

        void IRenderer.PopQuadBatch()
        {
        }

        event Action<Texture>? IRenderer.TextureCreated
        {
            add
            {
            }
            remove
            {
            }
        }

        Texture[] IRenderer.GetAllTextures() => Array.Empty<Texture>();
    }
}
