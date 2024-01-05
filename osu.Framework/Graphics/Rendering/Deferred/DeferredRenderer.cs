// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public object GetReference(RendererResource resource)
            => allocator.GetReference(resource);

        public RendererMemoryBlock Allocate<T>()
            where T : unmanaged
            => allocator.Allocate<T>();

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
        public bool IsInitialised => baseRenderer.IsInitialised;

        public IVertexBatch<TexturedVertex2D> DefaultQuadBatch { get; }

        private readonly Dictionary<Type, object> batchesByType = new Dictionary<Type, object>();

        private readonly IRenderer baseRenderer;
        private readonly Stack<RectangleI> viewportStack = new Stack<RectangleI>();
        private readonly Stack<RectangleI> scissorRectStack = new Stack<RectangleI>();
        private readonly Stack<Vector2I> scissorOffsetStack = new Stack<Vector2I>();
        private readonly Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        private readonly Stack<MaskingInfo> maskingInfoStack = new Stack<MaskingInfo>();
        private readonly Stack<DepthInfo> depthInfoStack = new Stack<DepthInfo>();
        private readonly Stack<StencilInfo> stencilInfoStack = new Stack<StencilInfo>();

        public DeferredRenderer(IRenderer baseRenderer)
        {
            this.baseRenderer = baseRenderer;

            DefaultQuadBatch = CreateQuadBatch<TexturedVertex2D>(100, 1000);
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

            FrameIndex++;
        }

        private readonly byte[] uploadBuffer = new byte[1024 * 1024];
        private VeldridMetalVertexBuffer<TexturedVertex2D>? vbo;

        public void FinishFrame()
        {
            baseRenderer.BeginFrame(windowSize);

            int currentUploadIndex = 0;

            EventListReader reader = renderEvents.CreateReader();

            while (reader.ReadType(out RenderEventType type))
            {
                switch (type)
                {
                    case RenderEventType.AddVertexToBatch:
                    {
                        AddVertexToBatchEvent e = reader.Read<AddVertexToBatchEvent>();
                        Span<byte> buffer = e.Data.GetBuffer(this);

                        if (currentUploadIndex + buffer.Length >= uploadBuffer.Length)
                            currentUploadIndex = 0;

                        buffer.CopyTo(uploadBuffer.AsSpan().Slice(currentUploadIndex));
                        currentUploadIndex += buffer.Length;
                        break;
                    }

                    default:
                        reader.Skip(type);
                        break;
                }
            }

            VeldridRenderer veldridRenderer = (VeldridRenderer)baseRenderer;

            ReadOnlySpan<TexturedVertex2D> verticesToDraw = MemoryMarshal.Cast<byte, TexturedVertex2D>(uploadBuffer.AsSpan().Slice(0, currentUploadIndex));
            int numVertices = verticesToDraw.Length;

            vbo ??= new VeldridMetalVertexBuffer<TexturedVertex2D>(veldridRenderer, IRenderer.MAX_QUADS);
            vbo.SetBuffer(verticesToDraw);

            veldridRenderer.BindVertexBuffer(vbo);
            veldridRenderer.BindIndexBuffer(VeldridIndexLayout.Quad, numVertices);

            int drawStartIndex = 0;
            int drawEndIndex = 0;

            reader = renderEvents.CreateReader();

            while (reader.ReadType(out RenderEventType type))
            {
                if (type == RenderEventType.AddVertexToBatch)
                {
                    reader.Skip(type);
                    drawEndIndex++;
                    continue;
                }

                if (drawStartIndex != drawEndIndex)
                {
                    veldridRenderer.DrawVertices(global::Veldrid.PrimitiveTopology.TriangleList, drawStartIndex, drawEndIndex - drawStartIndex);
                    drawStartIndex = drawEndIndex;
                }

                switch (type)
                {
                    case RenderEventType.BindFrameBuffer:
                        reader.Read<BindFrameBufferEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.BindShader:
                        reader.Read<BindShaderEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.BindTexture:
                        reader.Read<BindTextureEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.BindUniformBlock:
                        reader.Read<BindUniformBlockEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.Clear:
                        reader.Read<ClearEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.Disposal:
                        reader.Read<DisposalEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.DrawVertexBatch:
                        reader.Read<DrawVertexBatchEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.ExpensiveOperation:
                        reader.Read<ExpensiveOperationEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopDepthInfo:
                        reader.Read<PopDepthInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopMaskingInfo:
                        reader.Read<PopMaskingInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopProjectionMatrix:
                        reader.Read<PopProjectionMatrixEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopQuadBatch:
                        reader.Read<PopQuadBatchEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopScissor:
                        reader.Read<PopScissorEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopScissorOffset:
                        reader.Read<PopScissorOffsetEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopScissorState:
                        reader.Read<PopScissorStateEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopStencilInfo:
                        reader.Read<PopStencilInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PopViewport:
                        reader.Read<PopViewportEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushDepthInfo:
                        reader.Read<PushDepthInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushMaskingInfo:
                        reader.Read<PushMaskingInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushProjectionMatrix:
                        reader.Read<PushProjectionMatrixEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushQuadBatch:
                        reader.Read<PushQuadBatchEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushScissor:
                        reader.Read<PushScissorEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushScissorOffset:
                        reader.Read<PushScissorOffsetEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushScissorState:
                        reader.Read<PushScissorStateEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushStencilInfo:
                        reader.Read<PushStencilInfoEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.PushViewport:
                        reader.Read<PushViewportEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.SetBlend:
                        reader.Read<SetBlendEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.SetBlendMask:
                        reader.Read<SetBlendMaskEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.SetUniformBufferData:
                        reader.Read<SetUniformBufferDataEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.UnbindFrameBuffer:
                        reader.Read<UnbindFrameBufferEvent>().Run(this, baseRenderer);
                        break;

                    case RenderEventType.UnbindShader:
                        reader.Read<UnbindShaderEvent>().Run(this, baseRenderer);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawStartIndex != drawEndIndex)
                veldridRenderer.DrawVertices(global::Veldrid.PrimitiveTopology.TriangleList, drawStartIndex, drawEndIndex - drawStartIndex);

            baseRenderer.FinishFrame();
        }

        public void SwapBuffers()
        {
            baseRenderer.SwapBuffers();
        }

        public void WaitUntilIdle() => baseRenderer.WaitUntilIdle();

        public void WaitUntilNextFrameReady() => baseRenderer.WaitUntilNextFrameReady();

        public void MakeCurrent() => baseRenderer.MakeCurrent();

        public void ClearCurrent() => MakeCurrent();

        public void FlushCurrentBatch(FlushBatchSource? source)
        {
            // Todo: We may want to track these.
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
        {
            CurrentWrapModeS = wrapModeS ?? texture.WrapModeS;
            CurrentWrapModeT = wrapModeT ?? texture.WrapModeT;
            EnqueueEvent(new BindTextureEvent(Reference(texture), unit, wrapModeS, wrapModeT));
            return true;
        }

        public void Clear(ClearInfo clearInfo) => EnqueueEvent(new ClearEvent(clearInfo));

        public void PushScissorState(bool enabled) => EnqueueEvent(new PushScissorStateEvent(enabled));

        public void PopScissorState() => EnqueueEvent(new PopScissorStateEvent());

        public void SetBlend(BlendingParameters blendingParameters) => EnqueueEvent(new SetBlendEvent(blendingParameters));

        public void SetBlendMask(BlendingMask blendingMask) => EnqueueEvent(new SetBlendMaskEvent(blendingMask));

        public void PushViewport(RectangleI viewport)
        {
            viewportStack.Push(Viewport);
            Viewport = viewport;
            EnqueueEvent(new PushViewportEvent(viewport));
        }

        public void PopViewport()
        {
            Viewport = viewportStack.Pop();
            EnqueueEvent(new PopViewportEvent());
        }

        public void PushScissor(RectangleI scissor)
        {
            scissorRectStack.Push(Scissor);
            Scissor = scissor;
            EnqueueEvent(new PushScissorEvent(scissor));
        }

        public void PopScissor()
        {
            Scissor = scissorRectStack.Pop();
            EnqueueEvent(new PopScissorEvent());
        }

        public void PushScissorOffset(Vector2I offset)
        {
            scissorOffsetStack.Push(ScissorOffset);
            ScissorOffset = offset;
            EnqueueEvent(new PushScissorOffsetEvent(offset));
        }

        public void PopScissorOffset()
        {
            ScissorOffset = scissorOffsetStack.Pop();
            EnqueueEvent(new PopScissorOffsetEvent());
        }

        public void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(ProjectionMatrix);
            ProjectionMatrix = matrix;
            EnqueueEvent(new PushProjectionMatrixEvent(matrix));
        }

        public void PopProjectionMatrix()
        {
            ProjectionMatrix = projectionMatrixStack.Pop();
            EnqueueEvent(new PopProjectionMatrixEvent());
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            maskingInfoStack.Push(currentMaskingInfo);
            currentMaskingInfo = maskingInfo;
            EnqueueEvent(new PushMaskingInfoEvent(maskingInfo));
        }

        public void PopMaskingInfo()
        {
            currentMaskingInfo = maskingInfoStack.Pop();
            EnqueueEvent(new PopMaskingInfoEvent());
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
            depthInfoStack.Push(CurrentDepthInfo);
            CurrentDepthInfo = depthInfo;
            EnqueueEvent(new PushDepthInfoEvent(depthInfo));
        }

        public void PopDepthInfo()
        {
            CurrentDepthInfo = depthInfoStack.Pop();
            EnqueueEvent(new PopDepthInfoEvent());
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
            stencilInfoStack.Push(CurrentStencilInfo);
            CurrentStencilInfo = stencilInfo;
            EnqueueEvent(new PushStencilInfoEvent(stencilInfo));
        }

        public void PopStencilInfo()
        {
            CurrentStencilInfo = stencilInfoStack.Pop();
            EnqueueEvent(new PopStencilInfoEvent());
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation) => EnqueueEvent(new ExpensiveOperationEvent(Reference(operation)));

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
            where T : class
            => EnqueueEvent(DisposalEvent.Create(this, target!, disposalAction));

        public Image<Rgba32> TakeScreenshot()
        {
            throw new NotImplementedException();
        }

        public IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => baseRenderer.CreateShaderPart(store, name, rawData, partType);

        public IShader CreateShader(string name, IShaderPart[] parts)
            => new DeferredShader(this, baseRenderer.CreateShader(name, parts));

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DeferredFrameBuffer(this, baseRenderer.CreateFrameBuffer(renderBufferFormats, filteringMode));

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Color4? initialisationColour = null)
            => baseRenderer.CreateTexture(width, height, manualMipmaps, filteringMode, wrapModeS, wrapModeT, initialisationColour);

        public Texture CreateVideoTexture(int width, int height)
            => baseRenderer.CreateVideoTexture(width, height);

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => throw new NotImplementedException();

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            where TVertex : unmanaged, IEquatable<TVertex>, IVertex
        {
            if (!batchesByType.TryGetValue(typeof(TVertex), out object? existing))
                existing = batchesByType[typeof(TVertex)] = new DeferredVertexBatch<TVertex>(this);

            return (IVertexBatch<TVertex>)existing;
        }

        public IUniformBuffer<TData> CreateUniformBuffer<TData>()
            where TData : unmanaged, IEquatable<TData>
            => new DeferredUniformBuffer<TData>(this, baseRenderer.CreateUniformBuffer<TData>());

        public IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            where TData : unmanaged, IEquatable<TData>
        {
            throw new NotImplementedException();
        }

        public void SetUniform<T>(IUniformWithValue<T> uniform) where T : unmanaged, IEquatable<T>
        {
            // Todo: Fine to not implement for now.
        }

        public void SetDrawDepth(float drawDepth)
        {
            BackbufferDrawDepth = drawDepth;
        }

        public void PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => EnqueueEvent(new PushQuadBatchEvent(Reference(quadBatch)));

        public void PopQuadBatch() => EnqueueEvent(new PopQuadBatchEvent());

        public event Action<Texture>? TextureCreated
        {
            add => baseRenderer.TextureCreated += value;
            remove => baseRenderer.TextureCreated -= value;
        }

        public Texture[] GetAllTextures() => baseRenderer.GetAllTextures();
    }
}
