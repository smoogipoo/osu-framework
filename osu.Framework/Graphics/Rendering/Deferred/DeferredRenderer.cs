// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Primitives;
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

        internal readonly List<IEvent> RenderEvents = new List<IEvent>();

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

            RenderEvents.Clear();

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

        public void FinishFrame()
        {
        }

        private readonly byte[] uploadBuffer = new byte[1024 * 1024];
        private VeldridMetalVertexBuffer<TexturedVertex2D>? vbo;

        public void SwapBuffers()
        {
            baseRenderer.BeginFrame(windowSize);

            int currentUploadIndex = 0;

            foreach (var e in RenderEvents)
            {
                switch (e)
                {
                    case IAddVertexToBatchEvent addVertexToBatchEvent:
                        if (currentUploadIndex + addVertexToBatchEvent.Stride >= uploadBuffer.Length)
                            currentUploadIndex = 0;

                        addVertexToBatchEvent.CopyTo(uploadBuffer.AsSpan().Slice(currentUploadIndex, addVertexToBatchEvent.Stride));
                        currentUploadIndex += addVertexToBatchEvent.Stride;
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

            foreach (var e in RenderEvents)
            {
                switch (e)
                {
                    case IAddVertexToBatchEvent:
                        drawEndIndex++;
                        continue;
                }

                if (drawStartIndex != drawEndIndex)
                {
                    veldridRenderer.DrawVertices(global::Veldrid.PrimitiveTopology.TriangleList, drawStartIndex, drawEndIndex - drawStartIndex);
                    drawStartIndex = drawEndIndex;
                }

                e.Run(this, baseRenderer);
            }

            if (drawStartIndex != drawEndIndex)
                veldridRenderer.DrawVertices(global::Veldrid.PrimitiveTopology.TriangleList, drawStartIndex, drawEndIndex - drawStartIndex);

            baseRenderer.FinishFrame();
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
            RenderEvents.Add(new BindTextureEvent(texture, unit, wrapModeS, wrapModeT));
            return true;
        }

        public void Clear(ClearInfo clearInfo) => RenderEvents.Add(new ClearEvent(clearInfo));

        public void PushScissorState(bool enabled) => RenderEvents.Add(new PushScissorStateEvent(enabled));

        public void PopScissorState() => RenderEvents.Add(new PopScissorStateEvent());

        public void SetBlend(BlendingParameters blendingParameters) => RenderEvents.Add(new SetBlendEvent(blendingParameters));

        public void SetBlendMask(BlendingMask blendingMask) => RenderEvents.Add(new SetBlendMaskEvent(blendingMask));

        public void PushViewport(RectangleI viewport)
        {
            viewportStack.Push(Viewport);
            Viewport = viewport;
            RenderEvents.Add(new PushViewportEvent(viewport));
        }

        public void PopViewport()
        {
            Viewport = viewportStack.Pop();
            RenderEvents.Add(new PopViewportEvent());
        }

        public void PushScissor(RectangleI scissor)
        {
            scissorRectStack.Push(Scissor);
            Scissor = scissor;
            RenderEvents.Add(new PushScissorEvent(scissor));
        }

        public void PopScissor()
        {
            Scissor = scissorRectStack.Pop();
            RenderEvents.Add(new PopScissorEvent());
        }

        public void PushScissorOffset(Vector2I offset)
        {
            scissorOffsetStack.Push(ScissorOffset);
            ScissorOffset = offset;
            RenderEvents.Add(new PushScissorOffsetEvent(offset));
        }

        public void PopScissorOffset()
        {
            ScissorOffset = scissorOffsetStack.Pop();
            RenderEvents.Add(new PopScissorOffsetEvent());
        }

        public void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(ProjectionMatrix);
            ProjectionMatrix = matrix;
            RenderEvents.Add(new PushProjectionMatrixEvent(matrix));
        }

        public void PopProjectionMatrix()
        {
            ProjectionMatrix = projectionMatrixStack.Pop();
            RenderEvents.Add(new PopProjectionMatrixEvent());
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            maskingInfoStack.Push(currentMaskingInfo);
            currentMaskingInfo = maskingInfo;
            RenderEvents.Add(new PushMaskingInfoEvent(maskingInfo));
        }

        public void PopMaskingInfo()
        {
            currentMaskingInfo = maskingInfoStack.Pop();
            RenderEvents.Add(new PopMaskingInfoEvent());
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
            depthInfoStack.Push(CurrentDepthInfo);
            CurrentDepthInfo = depthInfo;
            RenderEvents.Add(new PushDepthInfoEvent(depthInfo));
        }

        public void PopDepthInfo()
        {
            CurrentDepthInfo = depthInfoStack.Pop();
            RenderEvents.Add(new PopDepthInfoEvent());
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
            stencilInfoStack.Push(CurrentStencilInfo);
            CurrentStencilInfo = stencilInfo;
            RenderEvents.Add(new PushStencilInfoEvent(stencilInfo));
        }

        public void PopStencilInfo()
        {
            CurrentStencilInfo = stencilInfoStack.Pop();
            RenderEvents.Add(new PopStencilInfoEvent());
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation) => RenderEvents.Add(new ExpensiveOperationEvent(operation));

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target) => RenderEvents.Add(new DisposalEvent<T>(target, disposalAction));

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

        public void PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => RenderEvents.Add(new PushQuadBatchEvent(quadBatch));

        public void PopQuadBatch() => RenderEvents.Add(new PopQuadBatchEvent());

        public event Action<Texture>? TextureCreated
        {
            add => baseRenderer.TextureCreated += value;
            remove => baseRenderer.TextureCreated -= value;
        }

        public Texture[] GetAllTextures() => baseRenderer.GetAllTextures();
    }
}
