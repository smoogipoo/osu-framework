// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Lists;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;
using ShaderType = osu.Framework.Graphics.Shaders.ShaderType;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid
{
    public class VeldridRenderer : IRenderer
    {
        /// <summary>
        /// The interval (in frames) before checking whether VBOs should be freed.
        /// VBOs may remain unused for at most double this length before they are recycled.
        /// </summary>
        private const int vbo_free_check_interval = 300;

        public int MaxTextureSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a graphics context, like headless CI.
        public int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a graphics context, like headless CI.
        public int MaxTexturesUploadedPerFrame { get; set; } = 32;
        public int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;
        public bool IsEmbedded => false;
        public ulong ResetId { get; private set; }
        public ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;
        public RectangleI Viewport { get; private set; }
        public RectangleF Ortho { get; private set; }
        public RectangleI Scissor { get; private set; }
        public Vector2I ScissorOffset { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public DepthInfo CurrentDepthInfo { get; private set; }
        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public bool IsMaskingActive => maskingStack.Count > 1;
        public float BackbufferDrawDepth { get; private set; }
        public bool UsingBackbuffer => frameBufferStack.Count > 0 && frameBufferStack.Peek() == BackbufferFramebuffer;
        public bool AtlasTextureIsBound { get; private set; }

        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private readonly Lazy<WhiteTexture> whiteTexture;

        public Graphics.Textures.Texture WhiteTexture => whiteTexture.Value;

        protected Framebuffer BackbufferFramebuffer { get; private set; } = null!;

        private readonly GlobalStatistic<int> statExpensiveOperationsQueued = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Expensive operation queue length");
        private readonly GlobalStatistic<int> statTextureUploadsQueued = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Texture upload queue length");
        private readonly GlobalStatistic<int> statTextureUploadsDequeued = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Texture uploads dequeued");
        private readonly GlobalStatistic<int> statTextureUploadsPerformed = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Texture uploads performed");

        private readonly ConcurrentQueue<ScheduledDelegate> expensiveOperationQueue = new ConcurrentQueue<ScheduledDelegate>();
        private readonly ConcurrentQueue<INativeTexture> textureUploadQueue = new ConcurrentQueue<INativeTexture>();
        private readonly GLDisposalQueue disposalQueue = new GLDisposalQueue();

        private readonly Scheduler resetScheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        private readonly Stack<IVertexBatch<TexturedVertex2D>> quadBatches = new Stack<IVertexBatch<TexturedVertex2D>>();
        private readonly List<IVertexBuffer> vertexBuffersInUse = new List<IVertexBuffer>();
        private readonly List<IVertexBatch> batchResetList = new List<IVertexBatch>();
        private readonly Stack<RectangleI> viewportStack = new Stack<RectangleI>();
        private readonly Stack<RectangleF> orthoStack = new Stack<RectangleF>();
        private readonly Stack<MaskingInfo> maskingStack = new Stack<MaskingInfo>();
        private readonly Stack<RectangleI> scissorRectStack = new Stack<RectangleI>();
        private readonly Stack<DepthInfo> depthStack = new Stack<DepthInfo>();
        private readonly Stack<Vector2I> scissorOffsetStack = new Stack<Vector2I>();
        private readonly Stack<IShader> shaderStack = new Stack<IShader>();
        private readonly Stack<bool> scissorStateStack = new Stack<bool>();
        private readonly Stack<Framebuffer> frameBufferStack = new Stack<Framebuffer>();
        private readonly int[] lastBoundBuffers = new int[2];

        private BlendingParameters lastBlendingParameters;
        private IVertexBatch? lastActiveBatch;
        private MaskingInfo currentMaskingInfo;
        private IShader? currentShader;
        private bool currentScissorState;
        private bool isInitialised;
        private IVertexBatch<TexturedVertex2D>? defaultQuadBatch;

        internal const uint UNIFORM_RESOURCE_SLOT = 0;
        internal const uint TEXTURE_RESOURCE_SLOT = 1;

        public GraphicsDevice Device { get; private set; } = null!;

        public ResourceFactory Factory => Device.ResourceFactory;

        public CommandList Commands { get; private set; } = null!;

        internal VeldridIndexData SharedLinearIndex { get; }
        internal VeldridIndexData SharedQuadIndex { get; }

        private ResourceLayout uniformLayout = null!;

        private VeldridTextureSamplerSet defaultTextureSet = null!;

        private VeldridTextureSamplerSet? boundTextureSet;

        private DeviceBuffer? boundVertexBuffer;
        // private VertexLayoutDescription? boundVertexLayout;

        internal static readonly ResourceLayoutDescription UNIFORM_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex));

        internal static readonly ResourceLayoutDescription TEXTURE_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment));

        private GraphicsPipelineDescription pipeline = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CullNone,
        };

        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        public VeldridRenderer()
        {
            whiteTexture = new Lazy<WhiteTexture>(() =>
                new TextureAtlas(this, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhiteTexture);

            SharedLinearIndex = new VeldridIndexData(this);
            SharedQuadIndex = new VeldridIndexData(this);
        }

        void IRenderer.Initialise()
        {
            // todo: port device creation logic (https://github.com/frenzibyte/osu-framework/blob/3e9458b007b1de1eaaa5f0483387c862f27bb331/osu.Framework/Graphics/Veldrid/Vd_Device.cs#L21-L240)
            // that requires further thought as it includes acquiring the current window and display handle.
            Device = null!;

            Commands = Factory.CreateCommandList();

            uniformLayout = Factory.CreateResourceLayout(UNIFORM_LAYOUT);

            BackbufferFramebuffer = Device.SwapchainFramebuffer;

            pipeline.ResourceLayouts = new ResourceLayout[2];
            pipeline.ResourceLayouts[UNIFORM_RESOURCE_SLOT] = uniformLayout;
            pipeline.Outputs = BackbufferFramebuffer.OutputDescription;

            var defaultTexture = Factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
            Device.UpdateTexture(defaultTexture, new ReadOnlySpan<Rgba32>(new[] { new Rgba32(0, 0, 0) }), 0, 0, 0, 1, 1, 1, 0, 0);
            defaultTextureSet = new VeldridTextureSamplerSet(this, defaultTexture, Device.LinearSampler);

            defaultQuadBatch = CreateQuadBatch<TexturedVertex2D>(100, 1000);

            resetScheduler.AddDelayed(checkPendingDisposals, 0, true);
            isInitialised = true;
        }

        private void checkPendingDisposals()
        {
            disposalQueue.CheckPendingDisposals();
        }

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private Pipeline getPipelineInstance()
        {
            if (!pipelineCache.TryGetValue(pipeline, out var instance))
            {
                pipelineCache[pipeline] = instance = Factory.CreateGraphicsPipeline(pipeline);
                stat_graphics_pipeline_created.Value++;
            }

            return instance;
        }

        private Vector2 currentSize;

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
            Debug.Assert(defaultQuadBatch != null);

            ResetId++;

            resetScheduler.Update();

            statExpensiveOperationsQueued.Value = expensiveOperationQueue.Count;

            while (expensiveOperationQueue.TryDequeue(out ScheduledDelegate? operation))
            {
                if (operation.State == ScheduledDelegate.RunState.Waiting)
                {
                    operation.RunTask();
                    break;
                }
            }

            lastActiveBatch = null;
            lastBlendingParameters = new BlendingParameters();

            foreach (var b in batchResetList)
                b.ResetCounters();
            batchResetList.Clear();

            currentShader?.Unbind();
            currentShader = null;
            shaderStack.Clear();

            viewportStack.Clear();
            orthoStack.Clear();
            maskingStack.Clear();
            scissorRectStack.Clear();
            frameBufferStack.Clear();
            depthStack.Clear();
            scissorStateStack.Clear();
            scissorOffsetStack.Clear();

            quadBatches.Clear();
            quadBatches.Push(defaultQuadBatch);

            if (windowSize != currentSize)
            {
                // todo: look for better window resize handling
                Device.MainSwapchain.Resize((uint)windowSize.X, (uint)windowSize.Y);

                // resizing swapchain could cause it to recreate the framebuffer,
                // update current backbuffer property to new instance.
                BackbufferFramebuffer = Device.SwapchainFramebuffer;

                currentSize = windowSize;
            }

            Commands.Begin();

            BindFrameBuffer(BackbufferFramebuffer);

            Scissor = RectangleI.Empty;
            ScissorOffset = Vector2I.Zero;
            Viewport = RectangleI.Empty;
            Ortho = RectangleF.Empty;

            PushScissorState(true);
            PushViewport(new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y));
            PushScissor(new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y));
            PushScissorOffset(Vector2I.Zero);
            PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y),
                MaskingRect = new RectangleF(0, 0, windowSize.X, windowSize.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
                CornerExponent = 2.5f,
            }, true);

            PushDepthInfo(DepthInfo.Default);
            Clear(new ClearInfo(Color4.Black));

            freeUnusedVertexBuffers();

            statTextureUploadsQueued.Value = textureUploadQueue.Count;
            statTextureUploadsDequeued.Value = 0;
            statTextureUploadsPerformed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(textureUploadQueue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (textureUploadQueue.TryDequeue(out INativeTexture? texture))
            {
                statTextureUploadsDequeued.Value++;

                texture.IsQueuedForUpload = false;

                if (!texture.Upload())
                    continue;

                statTextureUploadsPerformed.Value++;

                if (++uploads >= targetUploads)
                    break;

                if ((uploadedPixels += texture.Width * texture.Height) > MaxPixelsUploadedPerFrame)
                    break;
            }

            lastBoundBuffers.AsSpan().Clear();
            // todo: this might not work, I'm not sure.
            boundTextureSet = defaultTextureSet;
        }

        void IRenderer.FinishFrame()
        {
            flushCurrentBatch();

            Commands.End();
            Device.SubmitCommands(Commands);
        }

        private void freeUnusedVertexBuffers()
        {
            if (ResetId % vbo_free_check_interval != 0)
                return;

            foreach (var buf in vertexBuffersInUse)
            {
                if (buf.InUse && ResetId - buf.LastUseResetId > vbo_free_check_interval)
                    buf.Free();
            }

            vertexBuffersInUse.RemoveAll(b => !b.InUse);
        }

        public void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            if (frameBufferStack.Peek().DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        public void PushScissorState(bool enabled)
        {
            scissorStateStack.Push(enabled);
            setScissorState(enabled);
        }

        public void PopScissorState()
        {
            Trace.Assert(scissorStateStack.Count > 1);

            scissorStateStack.Pop();

            setScissorState(scissorStateStack.Peek());
        }

        private void setScissorState(bool enabled)
        {
            if (enabled == currentScissorState)
                return;

            currentScissorState = enabled;

            pipeline.RasterizerState.ScissorTestEnabled = enabled;
        }

        public void BindVertexBuffer(DeviceBuffer vertex, VertexLayoutDescription layout)
        {
            if (vertex == boundVertexBuffer)
                return;

            Commands.SetVertexBuffer(0, vertex);

            // if (currentShader.VertexLayout.Elements == null || currentShader.VertexLayout.Elements.Length == 0)
            //     pipeline.ShaderSet.VertexLayouts = new[] { layout };

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexBuffer = vertex;
            // boundVertexLayout = layout;
        }

        public void BindIndexBuffer(DeviceBuffer index, IndexFormat format) => Commands.SetIndexBuffer(index, format);

        public bool BindTexture(Graphics.Textures.Texture texture, TextureUnit unit = TextureUnit.Texture0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
        {
            if (texture is WhiteTexture && AtlasTextureIsBound)
            {
                // We can use the special white space from any atlas texture.
                return true;
            }

            bool didBind = texture.Bind(unit, wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);
            AtlasTextureIsBound = texture is TextureAtlasRegion;

            return didBind;
        }

        public bool BindTexture(VeldridTextureSamplerSet textureSet, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (wrapModeS != CurrentWrapModeS)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeS, (int)wrapModeS);
                CurrentWrapModeS = wrapModeS;
            }

            if (wrapModeT != CurrentWrapModeT)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeT, (int)wrapModeT);
                CurrentWrapModeT = wrapModeT;
            }

            if (boundTextureSet == textureSet)
                return false;

            flushCurrentBatch();

            pipeline.ResourceLayouts[TEXTURE_RESOURCE_SLOT] = textureSet.Layout;
            boundTextureSet = textureSet;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        /// <summary>
        /// Updates a <see cref="Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The textural data.</param>
        /// <param name="bufferRowLength">An optional length per row on the given <paramref name="data"/>.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        public unsafe void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data, int? bufferRowLength = null)
            where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                if (bufferRowLength != null)
                {
                    var staging = Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, texture.Format, TextureUsage.Staging));

                    for (uint yi = 0; yi < height; yi++)
                        Device.UpdateTexture(staging, (IntPtr)(ptr + yi * bufferRowLength.Value), (uint)width, 0, yi, 0, (uint)width, 1, 1, 0, 0);

                    Commands.CopyTexture(staging, texture);
                    staging.Dispose();
                }
                else
                    Device.UpdateTexture(texture, (IntPtr)ptr, (uint)(data.Length * sizeof(T)), (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            }
        }

        private static readonly Dictionary<int, ResourceLayout> texture_layouts = new Dictionary<int, ResourceLayout>();

        /// <summary>
        /// Retrieves a <see cref="ResourceLayout"/> for a texture-sampler resource set.
        /// </summary>
        /// <param name="textureCount">The number of textures in the resource layout.</param>
        /// <returns></returns>
        public ResourceLayout GetTextureResourceLayout(int textureCount)
        {
            if (texture_layouts.TryGetValue(textureCount, out var layout))
                return layout;

            var description = new ResourceLayoutDescription(new ResourceLayoutElementDescription[textureCount + 1]);
            var textureElement = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.TextureReadOnly);

            for (int i = 0; i < textureCount; i++)
                description.Elements[i] = new ResourceLayoutElementDescription($"{textureElement.Name}{i}", textureElement.Kind, textureElement.Stages);

            description.Elements[^1] = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.Sampler);
            return texture_layouts[textureCount] = Factory.CreateResourceLayout(description);
        }

        public void DrawVertices(PrimitiveTopology type, int indexStart, int indicesCount)
        {
            pipeline.PrimitiveTopology = type;

            Commands.SetPipeline(getPipelineInstance());
            // Commands.SetGraphicsResourceSet(UNIFORM_RESOURCE_SLOT, currentShader.UniformResourceSet);
            Commands.SetGraphicsResourceSet(TEXTURE_RESOURCE_SLOT, boundTextureSet);

            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
        }

        public void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters == blendingParameters)
                return;

            flushCurrentBatch();

            pipeline.BlendState = new BlendStateDescription(default, blendingParameters.ToBlendAttachment());
            lastBlendingParameters = blendingParameters;
        }

        public void PushViewport(RectangleI viewport)
        {
            var actualRect = viewport;

            if (actualRect.Width < 0)
            {
                actualRect.X += viewport.Width;
                actualRect.Width = -viewport.Width;
            }

            if (actualRect.Height < 0)
            {
                actualRect.Y += viewport.Height;
                actualRect.Height = -viewport.Height;
            }

            PushOrtho(viewport);

            viewportStack.Push(actualRect);

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;
            setViewport(actualRect);
        }

        public void PopViewport()
        {
            Trace.Assert(viewportStack.Count > 1);

            PopOrtho();

            viewportStack.Pop();
            RectangleI actualRect = viewportStack.Peek();

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;
            setViewport(actualRect);
        }

        private void setViewport(RectangleI viewport)
        {
            Commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));
        }

        public void PushScissor(RectangleI scissor)
        {
            flushCurrentBatch();

            scissorRectStack.Push(scissor);
            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
        }

        public void PopScissor()
        {
            Trace.Assert(scissorRectStack.Count > 1);

            flushCurrentBatch();

            scissorRectStack.Pop();
            RectangleI scissor = scissorRectStack.Peek();

            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
        }

        private void setScissor(RectangleI scissor)
        {
            if (scissor.Width < 0)
            {
                scissor.X += scissor.Width;
                scissor.Width = -scissor.Width;
            }

            if (scissor.Height < 0)
            {
                scissor.Y += scissor.Height;
                scissor.Height = -scissor.Height;
            }

            Commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);
        }

        public void PushScissorOffset(Vector2I offset)
        {
            flushCurrentBatch();

            scissorOffsetStack.Push(offset);
            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        public void PopScissorOffset()
        {
            Trace.Assert(scissorOffsetStack.Count > 1);

            flushCurrentBatch();

            scissorOffsetStack.Pop();
            Vector2I offset = scissorOffsetStack.Peek();

            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        public void PushOrtho(RectangleF ortho)
        {
            flushCurrentBatch();

            orthoStack.Push(ortho);
            if (Ortho == ortho)
                return;

            Ortho = ortho;
            setProjectionMatrix(ortho);
        }

        public void PopOrtho()
        {
            Trace.Assert(orthoStack.Count > 1);

            flushCurrentBatch();

            orthoStack.Pop();
            RectangleF actualRect = orthoStack.Peek();

            if (Ortho == actualRect)
                return;

            Ortho = actualRect;
            setProjectionMatrix(actualRect);
        }

        private void setProjectionMatrix(RectangleF ortho)
        {
            // Inverse the near/far values to not affect with depth values during multiplication.
            // todo: replace this with a custom implementation or otherwise.
            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(ortho.Left, ortho.Right, ortho.Bottom, ortho.Top, 1, -1);

            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            maskingStack.Push(maskingInfo);
            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, true, overwritePreviousScissor);
        }

        public void PopMaskingInfo()
        {
            Trace.Assert(maskingStack.Count > 1);

            maskingStack.Pop();
            MaskingInfo maskingInfo = maskingStack.Peek();

            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, false, true);
        }

        private void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing, bool overwritePreviousScissor)
        {
            flushCurrentBatch();

            GlobalPropertyManager.Set(GlobalProperty.MaskingRect, new Vector4(
                maskingInfo.MaskingRect.Left,
                maskingInfo.MaskingRect.Top,
                maskingInfo.MaskingRect.Right,
                maskingInfo.MaskingRect.Bottom));

            GlobalPropertyManager.Set(GlobalProperty.ToMaskingSpace, maskingInfo.ToMaskingSpace);

            GlobalPropertyManager.Set(GlobalProperty.CornerRadius, maskingInfo.CornerRadius);
            GlobalPropertyManager.Set(GlobalProperty.CornerExponent, maskingInfo.CornerExponent);

            GlobalPropertyManager.Set(GlobalProperty.BorderThickness, maskingInfo.BorderThickness / maskingInfo.BlendRange);

            if (maskingInfo.BorderThickness > 0)
            {
                GlobalPropertyManager.Set(GlobalProperty.BorderColour, new Matrix4(
                    // TopLeft
                    maskingInfo.BorderColour.TopLeft.Linear.R,
                    maskingInfo.BorderColour.TopLeft.Linear.G,
                    maskingInfo.BorderColour.TopLeft.Linear.B,
                    maskingInfo.BorderColour.TopLeft.Linear.A,
                    // BottomLeft
                    maskingInfo.BorderColour.BottomLeft.Linear.R,
                    maskingInfo.BorderColour.BottomLeft.Linear.G,
                    maskingInfo.BorderColour.BottomLeft.Linear.B,
                    maskingInfo.BorderColour.BottomLeft.Linear.A,
                    // TopRight
                    maskingInfo.BorderColour.TopRight.Linear.R,
                    maskingInfo.BorderColour.TopRight.Linear.G,
                    maskingInfo.BorderColour.TopRight.Linear.B,
                    maskingInfo.BorderColour.TopRight.Linear.A,
                    // BottomRight
                    maskingInfo.BorderColour.BottomRight.Linear.R,
                    maskingInfo.BorderColour.BottomRight.Linear.G,
                    maskingInfo.BorderColour.BottomRight.Linear.B,
                    maskingInfo.BorderColour.BottomRight.Linear.A));
            }

            GlobalPropertyManager.Set(GlobalProperty.MaskingBlendRange, maskingInfo.BlendRange);
            GlobalPropertyManager.Set(GlobalProperty.AlphaExponent, maskingInfo.AlphaExponent);

            GlobalPropertyManager.Set(GlobalProperty.EdgeOffset, maskingInfo.EdgeOffset);

            GlobalPropertyManager.Set(GlobalProperty.DiscardInner, maskingInfo.Hollow);
            if (maskingInfo.Hollow)
                GlobalPropertyManager.Set(GlobalProperty.InnerCornerRadius, maskingInfo.HollowCornerRadius);

            if (isPushing)
            {
                // When drawing to a viewport that doesn't match the projection size (e.g. via framebuffers), the resultant image will be scaled
                Vector2 viewportScale = Vector2.Divide(Viewport.Size, Ortho.Size);

                Vector2 location = (maskingInfo.ScreenSpaceAABB.Location - ScissorOffset) * viewportScale;
                Vector2 size = maskingInfo.ScreenSpaceAABB.Size * viewportScale;

                RectangleI actualRect = new RectangleI(
                    (int)Math.Floor(location.X),
                    (int)Math.Floor(location.Y),
                    (int)Math.Ceiling(size.X),
                    (int)Math.Ceiling(size.Y));

                PushScissor(overwritePreviousScissor ? actualRect : RectangleI.Intersect(scissorRectStack.Peek(), actualRect));
            }
            else
                PopScissor();
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
            depthStack.Push(depthInfo);

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        public void PopDepthInfo()
        {
            Trace.Assert(depthStack.Count > 1);

            depthStack.Pop();
            DepthInfo depthInfo = depthStack.Peek();

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        private void setDepthInfo(DepthInfo depthInfo)
        {
            flushCurrentBatch();

            pipeline.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipeline.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipeline.DepthStencilState.DepthComparison = depthInfo.Function.ToComparisonKind();
        }

        public void BindFrameBuffer(Framebuffer frameBuffer)
        {
            bool alreadyBound = frameBufferStack.Count > 0 && frameBufferStack.Peek() == frameBuffer;

            frameBufferStack.Push(frameBuffer);

            if (!alreadyBound)
            {
                flushCurrentBatch();

                Commands.SetFramebuffer(frameBuffer);
                pipeline.Outputs = frameBuffer.OutputDescription;

                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UnbindFrameBuffer(Framebuffer frameBuffer)
        {
            if (frameBufferStack.Peek() != frameBuffer)
                return;

            frameBufferStack.Pop();

            flushCurrentBatch();

            Commands.SetFramebuffer(frameBufferStack.Peek());
            pipeline.Outputs = frameBufferStack.Peek().OutputDescription;

            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UseProgram(IShader? shader)
        {
            ThreadSafety.EnsureDrawThread();

            if (shader != null)
                shaderStack.Push(shader);
            else
            {
                shaderStack.Pop();

                //check if the stack is empty, and if so don't restore the previous shader.
                if (shaderStack.Count == 0)
                    return;
            }

            shader ??= shaderStack.Peek();

            if (currentShader == shader)
                return;

            FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

            flushCurrentBatch();

            // todo: support Veldrid shaders once IRenderer supports creating its own shaders
            // pipelineDescription.ShaderSet.Shaders = shader.Shaders;

            // if (shader.VertexLayout.Elements?.Length > 0)
            // pipelineDescription.ShaderSet.VertexLayouts = new[] { shader.VertexLayout };

            currentShader = shader;
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation)
        {
            if (isInitialised)
                expensiveOperationQueue.Enqueue(operation);
        }

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            if (isInitialised)
                disposalQueue.ScheduleDisposal(disposalAction, target);
            else
                disposalAction.Invoke(target);
        }

        void IRenderer.EnqueueTextureUpload(INativeTexture texture)
        {
            if (texture.IsQueuedForUpload)
                return;

            if (isInitialised)
            {
                texture.IsQueuedForUpload = true;
                textureUploadQueue.Enqueue(texture);
            }
        }

        public IFrameBuffer CreateFrameBuffer(RenderbufferInternalFormat[]? renderBufferFormats = null, All filteringMode = All.Linear)
            => throw new NotImplementedException();

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveType primitiveType) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new VeldridLinearBatch<TVertex>(this, size, maxBuffers, primitiveType.ToPrimitiveTopology());

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
            => new VeldridQuadBatch<TVertex>(this, size, maxBuffers);

        public Graphics.Textures.Texture CreateTexture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, WrapMode wrapModeS = WrapMode.None,
                                                       WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
            => createTexture(new VeldridTexture(this, width, height, manualMipmaps, filteringMode, initialisationColour), wrapModeS, wrapModeT);

        public Graphics.Textures.Texture CreateVideoTexture(int width, int height)
            => throw new NotImplementedException();

        IShaderPart IRenderer.CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderType type)
            => throw new NotImplementedException();

        IShader IRenderer.CreateShader(string name, params IShaderPart[] parts)
            => throw new NotImplementedException();

        private Graphics.Textures.Texture createTexture(VeldridTexture texture, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            var tex = new Graphics.Textures.Texture(texture, wrapModeS, wrapModeT);

            allTextures.Add(tex);
            TextureCreated?.Invoke(tex);

            return tex;
        }

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
            if (uniform.Owner == currentShader)
                flushCurrentBatch();

            // todo: pending veldrid shader support, reference code: https://github.com/frenzibyte/osu-framework/blob/3e9458b007b1de1eaaa5f0483387c862f27bb331/osu.Framework/Graphics/Veldrid/Vd_Resources.cs#L267-L298
            // switch (uniform)
            // {
            //     case IUniformWithValue<bool> b:
            //         GL.Uniform1(uniform.Location, b.GetValue() ? 1 : 0);
            //         break;
            //
            //     case IUniformWithValue<int> i:
            //         GL.Uniform1(uniform.Location, i.GetValue());
            //         break;
            //
            //     case IUniformWithValue<float> f:
            //         GL.Uniform1(uniform.Location, f.GetValue());
            //         break;
            //
            //     case IUniformWithValue<Vector2> v2:
            //         GL.Uniform2(uniform.Location, ref v2.GetValueByRef());
            //         break;
            //
            //     case IUniformWithValue<Vector3> v3:
            //         GL.Uniform3(uniform.Location, ref v3.GetValueByRef());
            //         break;
            //
            //     case IUniformWithValue<Vector4> v4:
            //         GL.Uniform4(uniform.Location, ref v4.GetValueByRef());
            //         break;
            //
            //     case IUniformWithValue<Matrix2> m2:
            //         GL.UniformMatrix2(uniform.Location, false, ref m2.GetValueByRef());
            //         break;
            //
            //     case IUniformWithValue<Matrix3> m3:
            //         GL.UniformMatrix3(uniform.Location, false, ref m3.GetValueByRef());
            //         break;
            //
            //     case IUniformWithValue<Matrix4> m4:
            //         GL.UniformMatrix4(uniform.Location, false, ref m4.GetValueByRef());
            //         break;
            // }
        }

        void IRenderer.RegisterVertexBufferUse(IVertexBuffer buffer) => RegisterVertexBufferUse(buffer);

        internal void RegisterVertexBufferUse(IVertexBuffer buffer) => vertexBuffersInUse.Add(buffer);

        void IRenderer.SetActiveBatch(IVertexBatch batch)
        {
            if (lastActiveBatch == batch)
                return;

            batchResetList.Add(batch);

            flushCurrentBatch();

            lastActiveBatch = batch;
        }

        void IRenderer.SetDrawDepth(float drawDepth) => BackbufferDrawDepth = drawDepth;

        IVertexBatch<TexturedVertex2D> IRenderer.DefaultQuadBatch => quadBatches.Peek();

        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => quadBatches.Push(quadBatch);

        void IRenderer.PopQuadBatch() => quadBatches.Pop();

        private readonly LockedWeakList<Graphics.Textures.Texture> allTextures = new LockedWeakList<Graphics.Textures.Texture>();

        internal event Action<Graphics.Textures.Texture>? TextureCreated;

        event Action<Graphics.Textures.Texture>? IRenderer.TextureCreated
        {
            add => TextureCreated += value;
            remove => TextureCreated -= value;
        }

        Graphics.Textures.Texture[] IRenderer.GetAllTextures() => allTextures.ToArray();

        private void flushCurrentBatch()
        {
            lastActiveBatch?.Draw();
        }
    }
}
