// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredRenderer : Renderer, IVeldridRenderer
    {
        private readonly ResourceAllocator allocator;
        private readonly EventList renderEvents;
        private readonly VertexManager vertexManager;
        private readonly UniformBufferManager uniformBufferManager;

        public ResourceReference Reference<T>(T obj)
            where T : class
            => allocator.Reference(obj);

        public object Dereference(ResourceReference reference)
            => allocator.Dereference(reference);

        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
            => allocator.AllocateObject(data);

        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
            => allocator.AllocateRegion(data);

        public MemoryReference AllocateRegion(int length)
            => allocator.AllocateRegion(length);

        public Span<byte> GetRegion(MemoryReference reference)
            => allocator.GetRegion(reference);

        public void EnqueueEvent<T>(in T @event)
            where T : unmanaged, IRenderEvent
            => renderEvents.Enqueue(@event);

        private readonly HashSet<IVeldridUniformBuffer> uniformBufferResetList = new HashSet<IVeldridUniformBuffer>();
        private readonly Stack<DrawNode> drawNodeStack = new Stack<DrawNode>();

        private EventProcessor processor = null!;
        private VeldridDevice veldridDevice = null!;

        public DeferredRenderer()
        {
            allocator = new ResourceAllocator();
            renderEvents = new EventList(allocator);
            vertexManager = new VertexManager(this);
            uniformBufferManager = new UniformBufferManager(this);
        }

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            veldridDevice = new VeldridDevice(graphicsSurface);
            processor = new EventProcessor(this, veldridDevice.Graphics, vertexManager, uniformBufferManager);

            MaxTextureSize = veldridDevice.MaxTextureSize;
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            foreach (var ubo in uniformBufferResetList)
                ubo.ResetCounters();
            uniformBufferResetList.Clear();

            allocator.Reset();
            renderEvents.Reset();
            vertexManager.Reset();
            uniformBufferManager.Reset();
            drawNodeStack.Clear();

            veldridDevice.BeginFrame(new Vector2I((int)windowSize.X, (int)windowSize.Y));

            base.BeginFrame(windowSize);
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            processor.ProcessEvents(renderEvents.CreateReader());

            veldridDevice.FinishFrame();
        }

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            if (texture == null)
                return false;

            EnqueueEvent(new SetTextureEvent(Reference(texture), unit));
            return true;
        }

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
        {
            if (frameBuffer == null)
                EnqueueEvent(new UnsetFrameBufferEvent());
            else
                EnqueueEvent(new SetFrameBufferEvent(Reference(frameBuffer)));
        }

        public override void DrawVerticesImplementation(PrimitiveTopology type, int vertexStart, int verticesCount)
        {
            // Handled by the batch...
        }

        protected override void ClearImplementation(ClearInfo clearInfo) => EnqueueEvent(new ClearEvent(clearInfo));

        protected override void SetScissorStateImplementation(bool enabled) => EnqueueEvent(new SetScissorStateEvent(enabled));

        protected override void SetBlendImplementation(BlendingParameters blendingParameters) => EnqueueEvent(new SetBlendEvent(blendingParameters));

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask) => EnqueueEvent(new SetBlendMaskEvent(blendingMask));

        protected override void SetViewportImplementation(RectangleI viewport) => EnqueueEvent(new SetViewportEvent(viewport));

        protected override void SetScissorImplementation(RectangleI scissor) => EnqueueEvent(new SetScissorEvent(scissor));

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo) => EnqueueEvent(new SetDepthInfoEvent(depthInfo));

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo) => EnqueueEvent(new SetStencilInfoEvent(stencilInfo));

        protected override void SetShaderImplementation(IShader shader) => EnqueueEvent(new SetShaderEvent(Reference(shader)));

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform) => throw new NotSupportedException();

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => new VeldridShaderPart(this, rawData, partType, store);

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
            => new DeferredShader(this, new VeldridShader(this, name, parts.Cast<VeldridShaderPart>().ToArray(), compilationStore));

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DeferredFrameBuffer(this, renderBufferFormats?.ToPixelFormats(), filteringMode.ToSamplerFilter());

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
            => new VeldridTexture(this, width, height, manualMipmaps, filteringMode.ToSamplerFilter(), initialisationColour);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
            => new VeldridVideoTexture(this, width, height);

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new DeferredVertexBatch<TVertex>(this, vertexManager, topology, IndexLayout.Linear);

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            => new DeferredVertexBatch<TVertex>(this, vertexManager, PrimitiveTopology.Triangles, IndexLayout.Quad);

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new DeferredUniformBuffer<TData>(this, uniformBufferManager);

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            => new DeferredShaderStorageBufferObject<TData>(this, ssboSize);

        void IVeldridRenderer.BindShader(VeldridShader shader) => BindShader(shader);

        void IVeldridRenderer.UnbindShader(VeldridShader shader) => UnbindShader(shader);

        void IVeldridRenderer.BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer) => veldridDevice.Graphics.AttachUniformBuffer(blockName, veldridBuffer);

        void IVeldridRenderer.UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            => veldridDevice.Graphics.UpdateTexture(texture, x, y, width, height, level, data);

        void IVeldridRenderer.UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
            => veldridDevice.Graphics.UpdateTexture(texture, x, y, width, height, level, data, rowLengthInBytes);

        CommandList IVeldridRenderer.BufferUpdateCommands => veldridDevice.Graphics.Commands;

        void IVeldridRenderer.EnqueueTextureUpload(VeldridTexture texture) => EnqueueTextureUpload(texture);

        void IVeldridRenderer.GenerateMipmaps(VeldridTexture texture) => veldridDevice.Graphics.Commands.GenerateMipmaps(texture.GetResourceList().Single().Texture);

        void IVeldridRenderer.BindFrameBuffer(VeldridFrameBuffer frameBuffer) => BindFrameBuffer(frameBuffer);

        void IVeldridRenderer.UnbindFrameBuffer(VeldridFrameBuffer frameBuffer) => UnbindFrameBuffer(frameBuffer);

        Textures.Texture IVeldridRenderer.CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS, WrapMode wrapModeT) => CreateTexture(nativeTexture, wrapModeS, wrapModeT);

        public void RegisterUniformBufferForReset(IVeldridUniformBuffer veldridUniformBuffer) => uniformBufferResetList.Add(veldridUniformBuffer);

        public bool UseStructuredBuffers => veldridDevice.UseStructuredBuffers;

        GraphicsSurfaceType IVeldridRenderer.SurfaceType => veldridDevice.SurfaceType;

        public ResourceFactory Factory => veldridDevice.Factory;

        public GraphicsDevice Device => veldridDevice.Device;

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

        protected internal override void SwapBuffers() => veldridDevice.SwapBuffers();

        protected internal override void WaitUntilIdle() => veldridDevice.WaitUntilIdle();

        protected internal override void WaitUntilNextFrameReady() => veldridDevice.WaitUntilNextFrameReady();

        protected internal override void MakeCurrent() => veldridDevice.MakeCurrent();

        protected internal override void ClearCurrent() => veldridDevice.ClearCurrent();

        protected internal override Image<Rgba32> TakeScreenshot() => veldridDevice.TakeScreenshot();

        void IRenderer.EnterDrawNode(DrawNode node)
        {
            drawNodeStack.Push(node);
            EnqueueEvent(new DrawNodeActionEvent(Reference(node), DrawNodeActionType.Enter));
        }

        void IRenderer.ExitDrawNode()
        {
            EnqueueEvent(new DrawNodeActionEvent(Reference(drawNodeStack.Pop()), DrawNodeActionType.Exit));
        }
    }
}
