// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredRenderer : IRenderer
    {
        private readonly ResourceAllocator allocator = new ResourceAllocator();
        private readonly EventList renderEvents = new EventList();

        public RendererResource Reference<T>(T obj)
            where T : class
            => allocator.Reference(obj);

        public object GetResource(RendererResource resource)
            => allocator.GetResource(resource);

        public RendererMemoryBlock Allocate<T>(T data)
            where T : unmanaged
            => allocator.Allocate(data);

        public Span<byte> GetBuffer(RendererMemoryBlock block)
            => allocator.GetBuffer(block);

        public void EnqueueEvent<T>(in T @event)
            where T : unmanaged, IRenderEvent
            => renderEvents.Enqueue(@event);

        public bool VerticalSync
        {
            get => baseRenderer.VerticalSync;
            set => baseRenderer.VerticalSync = value;
        }

        public bool AllowTearing
        {
            get => baseRenderer.AllowTearing;
            set => baseRenderer.AllowTearing = value;
        }

        public Storage? CacheStorage
        {
            set => baseRenderer.CacheStorage = value;
        }

        event Action<FlushBatchSource?>? IRenderer.OnFlush
        {
            add { }
            remove { }
        }

        public ulong FrameIndex { get; private set; }

        public int MaxTextureSize => baseRenderer.MaxTextureSize;

        public int MaxTexturesUploadedPerFrame
        {
            get => baseRenderer.MaxTexturesUploadedPerFrame;
            set => baseRenderer.MaxTexturesUploadedPerFrame = value;
        }

        public int MaxPixelsUploadedPerFrame
        {
            get => baseRenderer.MaxPixelsUploadedPerFrame;
            set => baseRenderer.MaxPixelsUploadedPerFrame = value;
        }

        public bool IsDepthRangeZeroToOne => baseRenderer.IsDepthRangeZeroToOne;
        public bool IsUvOriginTopLeft => baseRenderer.IsUvOriginTopLeft;
        public bool IsClipSpaceYInverted => baseRenderer.IsClipSpaceYInverted;

        public ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;

        private MaskingInfo currentMaskingInfo;

        public RectangleI Viewport { get; private set; }
        public RectangleI Scissor { get; private set; }
        public Vector2I ScissorOffset { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public DepthInfo CurrentDepthInfo { get; private set; }
        public StencilInfo CurrentStencilInfo { get; private set; }
        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public bool IsMaskingActive => maskingInfoStack.Count > 0;
        public float BackbufferDrawDepth { get; private set; }
        public bool UsingBackbuffer => true; // Todo: This is wrong.
        public Texture WhitePixel => baseRenderer.WhitePixel;
        public DepthValue BackbufferDepth => baseRenderer.BackbufferDepth;
        public bool IsInitialised => baseRenderer.IsInitialised;

        public IVertexBatch<TexturedVertex2D> DefaultQuadBatch { get; }

        private readonly Dictionary<DeferredVertexBatchLookup, IDeferredVertexBatch> deferredBatches = new Dictionary<DeferredVertexBatchLookup, IDeferredVertexBatch>();

        private readonly IRenderer baseRenderer;
        private readonly Stack<RectangleI> viewportStack = new Stack<RectangleI>();
        private readonly Stack<RectangleI> scissorRectStack = new Stack<RectangleI>();
        private readonly Stack<Vector2I> scissorOffsetStack = new Stack<Vector2I>();
        private readonly Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        private readonly Stack<MaskingInfo> maskingInfoStack = new Stack<MaskingInfo>();
        private readonly Stack<DepthInfo> depthInfoStack = new Stack<DepthInfo>();
        private readonly Stack<StencilInfo> stencilInfoStack = new Stack<StencilInfo>();

        private EventPainter painter;

        public DeferredRenderer(IRenderer baseRenderer)
        {
            this.baseRenderer = baseRenderer;

            DefaultQuadBatch = ((IRenderer)this).CreateQuadBatch<TexturedVertex2D>(100, 1000);

            baseRenderer.OnFlush += s => painter.FlushCurrentBatch(s);
        }

        public void Initialise(IGraphicsSurface graphicsSurface)
        {
            baseRenderer.Initialise(graphicsSurface);
        }

        private Vector2 windowSize;

        public void BeginFrame(Vector2 windowSize)
        {
            this.windowSize = windowSize;

            allocator.Reset();
            renderEvents.Reset();

            viewportStack.Clear();
            scissorRectStack.Clear();
            scissorOffsetStack.Clear();
            projectionMatrixStack.Clear();
            maskingInfoStack.Clear();
            depthInfoStack.Clear();
            stencilInfoStack.Clear();
            currentMaskingInfo = default;

            foreach ((_, IDeferredVertexBatch batch) in deferredBatches)
                batch.ResetCounters();

            FrameIndex++;
        }

        public void FinishFrame()
        {
            foreach ((_, IDeferredVertexBatch batch) in deferredBatches)
                batch.Prepare();

            baseRenderer.BeginFrame(windowSize);

            painter = new EventPainter(this, baseRenderer);
            painter.ProcessEvents(renderEvents.CreateReader());
            painter.Finish();

            baseRenderer.FinishFrame();
        }

        internal VeldridMetalVertexBuffer<TVertex> CreateVertexBuffer<TVertex>(int size)
            where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new VeldridMetalVertexBuffer<TVertex>((VeldridRenderer)baseRenderer, size);

        internal void DrawVertexBuffer<TVertex>(VeldridMetalVertexBuffer<TVertex> vbo, IndexLayout layout, PrimitiveTopology topology, int offset, int count)
            where TVertex : unmanaged, IEquatable<TVertex>, IVertex
        {
            VeldridRenderer veldridRenderer = (VeldridRenderer)baseRenderer;

            VeldridIndexLayout veldridLayout = layout switch
            {
                IndexLayout.Linear => VeldridIndexLayout.Linear,
                IndexLayout.Quad => VeldridIndexLayout.Quad,
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
            };

            veldridRenderer.BindVertexBuffer(vbo);
            veldridRenderer.BindIndexBuffer(veldridLayout, vbo.Size);
            veldridRenderer.DrawVertices(topology.ToPrimitiveTopology(), offset, count);
        }

        #region IRenderer Implementation

        void IRenderer.SwapBuffers() => baseRenderer.SwapBuffers();

        void IRenderer.WaitUntilIdle() => baseRenderer.WaitUntilIdle();

        void IRenderer.WaitUntilNextFrameReady() => baseRenderer.WaitUntilNextFrameReady();

        void IRenderer.MakeCurrent() => baseRenderer.MakeCurrent();

        void IRenderer.ClearCurrent() => ((IRenderer)this).MakeCurrent();

        void IRenderer.FlushCurrentBatch(FlushBatchSource? source) => baseRenderer.FlushCurrentBatch(source);

        bool IRenderer.BindTexture(Texture texture, int unit, WrapMode? wrapModeS, WrapMode? wrapModeT)
        {
            CurrentWrapModeS = wrapModeS ?? texture.WrapModeS;
            CurrentWrapModeT = wrapModeT ?? texture.WrapModeT;
            EnqueueEvent(new BindTextureEvent(Reference(texture), unit, wrapModeS, wrapModeT));
            return true;
        }

        void IRenderer.Clear(ClearInfo clearInfo) => EnqueueEvent(new ClearEvent(clearInfo));

        void IRenderer.PushScissorState(bool enabled) => EnqueueEvent(new PushScissorStateEvent(enabled));

        void IRenderer.PopScissorState() => EnqueueEvent(new PopScissorStateEvent());

        void IRenderer.SetBlend(BlendingParameters blendingParameters) => EnqueueEvent(new SetBlendEvent(blendingParameters));

        void IRenderer.SetBlendMask(BlendingMask blendingMask) => EnqueueEvent(new SetBlendMaskEvent(blendingMask));

        void IRenderer.PushViewport(RectangleI viewport)
        {
            viewportStack.Push(Viewport);
            Viewport = viewport;
            EnqueueEvent(new PushViewportEvent(viewport));
        }

        void IRenderer.PopViewport()
        {
            Viewport = viewportStack.Pop();
            EnqueueEvent(new PopViewportEvent());
        }

        void IRenderer.PushScissor(RectangleI scissor)
        {
            scissorRectStack.Push(Scissor);
            Scissor = scissor;
            EnqueueEvent(new PushScissorEvent(scissor));
        }

        void IRenderer.PopScissor()
        {
            Scissor = scissorRectStack.Pop();
            EnqueueEvent(new PopScissorEvent());
        }

        void IRenderer.PushScissorOffset(Vector2I offset)
        {
            scissorOffsetStack.Push(ScissorOffset);
            ScissorOffset = offset;
            EnqueueEvent(new PushScissorOffsetEvent(offset));
        }

        void IRenderer.PopScissorOffset()
        {
            ScissorOffset = scissorOffsetStack.Pop();
            EnqueueEvent(new PopScissorOffsetEvent());
        }

        void IRenderer.PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(ProjectionMatrix);
            ProjectionMatrix = matrix;
            EnqueueEvent(new PushProjectionMatrixEvent(matrix));
        }

        void IRenderer.PopProjectionMatrix()
        {
            ProjectionMatrix = projectionMatrixStack.Pop();
            EnqueueEvent(new PopProjectionMatrixEvent());
        }

        void IRenderer.PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor)
        {
            maskingInfoStack.Push(currentMaskingInfo);
            currentMaskingInfo = maskingInfo;
            EnqueueEvent(new PushMaskingInfoEvent(maskingInfo));
        }

        void IRenderer.PopMaskingInfo()
        {
            currentMaskingInfo = maskingInfoStack.Pop();
            EnqueueEvent(new PopMaskingInfoEvent());
        }

        void IRenderer.PushDepthInfo(DepthInfo depthInfo)
        {
            depthInfoStack.Push(CurrentDepthInfo);
            CurrentDepthInfo = depthInfo;
            EnqueueEvent(new PushDepthInfoEvent(depthInfo));
        }

        void IRenderer.PopDepthInfo()
        {
            CurrentDepthInfo = depthInfoStack.Pop();
            EnqueueEvent(new PopDepthInfoEvent());
        }

        void IRenderer.PushStencilInfo(StencilInfo stencilInfo)
        {
            stencilInfoStack.Push(CurrentStencilInfo);
            CurrentStencilInfo = stencilInfo;
            EnqueueEvent(new PushStencilInfoEvent(stencilInfo));
        }

        void IRenderer.PopStencilInfo()
        {
            CurrentStencilInfo = stencilInfoStack.Pop();
            EnqueueEvent(new PopStencilInfoEvent());
        }

        void IRenderer.ScheduleExpensiveOperation(ScheduledDelegate operation)
            => baseRenderer.ScheduleExpensiveOperation(operation);

        void IRenderer.ScheduleDisposal<T>(Action<T> disposalAction, T target)
            where T : class
            => baseRenderer.ScheduleDisposal(disposalAction, target);

        Image<Rgba32> IRenderer.TakeScreenshot() => throw new NotImplementedException();

        IShaderPart IRenderer.CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => baseRenderer.CreateShaderPart(store, name, rawData, partType);

        IShader IRenderer.CreateShader(string name, IShaderPart[] parts)
            => new DeferredShader(this, baseRenderer.CreateShader(name, parts));

        IFrameBuffer IRenderer.CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats, TextureFilteringMode filteringMode)
            => new DeferredFrameBuffer(this, baseRenderer.CreateFrameBuffer(renderBufferFormats, filteringMode));

        Texture IRenderer.CreateTexture(int width, int height, bool manualMipmaps, TextureFilteringMode filteringMode, WrapMode wrapModeS, WrapMode wrapModeT, Color4? initialisationColour)
            => baseRenderer.CreateTexture(width, height, manualMipmaps, filteringMode, wrapModeS, wrapModeT, initialisationColour);

        Texture IRenderer.CreateVideoTexture(int width, int height)
            => baseRenderer.CreateVideoTexture(width, height);

        IVertexBatch<TVertex> IRenderer.CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
        {
            DeferredVertexBatchLookup lookup = new DeferredVertexBatchLookup(typeof(TVertex), topology, IndexLayout.Linear);

            if (!deferredBatches.TryGetValue(lookup, out IDeferredVertexBatch? existing))
                existing = deferredBatches[lookup] = new DeferredVertexBatch<TVertex>(this, topology, IndexLayout.Linear);

            return (IVertexBatch<TVertex>)existing;
        }

        IVertexBatch<TVertex> IRenderer.CreateQuadBatch<TVertex>(int size, int maxBuffers)
        {
            DeferredVertexBatchLookup lookup = new DeferredVertexBatchLookup(typeof(TVertex), PrimitiveTopology.Triangles, IndexLayout.Quad);

            if (!deferredBatches.TryGetValue(lookup, out IDeferredVertexBatch? existing))
                existing = deferredBatches[lookup] = new DeferredVertexBatch<TVertex>(this, PrimitiveTopology.Triangles, IndexLayout.Quad);

            return (IVertexBatch<TVertex>)existing;
        }

        IUniformBuffer<TData> IRenderer.CreateUniformBuffer<TData>()
            => new DeferredUniformBuffer<TData>(this, baseRenderer.CreateUniformBuffer<TData>());

        IShaderStorageBufferObject<TData> IRenderer.CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize) => throw new NotImplementedException();

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
            // Todo: Fine to not implement for now.
        }

        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => EnqueueEvent(new PushQuadBatchEvent(Reference(quadBatch)));

        void IRenderer.PopQuadBatch() => EnqueueEvent(new PopQuadBatchEvent());

        event Action<Texture>? IRenderer.TextureCreated
        {
            add => baseRenderer.TextureCreated += value;
            remove => baseRenderer.TextureCreated -= value;
        }

        Texture[] IRenderer.GetAllTextures() => baseRenderer.GetAllTextures();

        #endregion
    }
}
