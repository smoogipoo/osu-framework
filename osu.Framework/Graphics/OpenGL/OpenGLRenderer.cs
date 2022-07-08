// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        /// <summary>
        /// The interval (in frames) before checking whether VBOs should be freed.
        /// VBOs may remain unused for at most double this length before they are recycled.
        /// </summary>
        private const int vbo_free_check_interval = 300;

        public int MaxTextureSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.
        public int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.
        public int MaxTexturesUploadedPerFrame { get; set; } = 32;
        public int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;
        public bool IsEmbedded { get; private set; }
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

        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private readonly Lazy<TextureWhitePixel> whitePixel;

        public Texture WhitePixel => whitePixel.Value;

        protected virtual int BackbufferFramebuffer => 0;

        private readonly GlobalStatistic<int> statExpensiveOperationsQueued = GlobalStatistics.Get<int>(nameof(OpenGLRenderer), "Expensive operation queue length");
        private readonly GlobalStatistic<int> statTextureUploadsQueued = GlobalStatistics.Get<int>(nameof(OpenGLRenderer), "Texture upload queue length");
        private readonly GlobalStatistic<int> statTextureUploadsDequeued = GlobalStatistics.Get<int>(nameof(OpenGLRenderer), "Texture uploads dequeued");
        private readonly GlobalStatistic<int> statTextureUploadsPerformed = GlobalStatistics.Get<int>(nameof(OpenGLRenderer), "Texture uploads performed");

        private readonly ConcurrentQueue<ScheduledDelegate> expensiveOperationQueue = new ConcurrentQueue<ScheduledDelegate>();
        private readonly ConcurrentQueue<ITexture> textureUploadQueue = new ConcurrentQueue<ITexture>();
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
        private readonly Stack<Shader> shaderStack = new Stack<Shader>();
        private readonly Stack<bool> scissorStateStack = new Stack<bool>();
        private readonly Stack<int> frameBufferStack = new Stack<int>();
        private readonly bool[] lastBoundTextureIsAtlas = new bool[16];
        private readonly int[] lastBoundBuffers = new int[2];
        private readonly int[] lastBoundTexture = new int[16];

        private BlendingParameters lastBlendingParameters;
        private IVertexBatch? lastActiveBatch;
        private TextureUnit lastActiveTextureUnit;
        private MaskingInfo currentMaskingInfo;
        private ClearInfo currentClearInfo;
        private Shader? currentShader;
        private bool? lastBlendingEnabledState;
        private bool currentScissorState;
        private bool isInitialised;
        private IVertexBatch<TexturedVertex2D>? defaultQuadBatch;

        public OpenGLRenderer()
        {
            whitePixel = new Lazy<TextureWhitePixel>(() =>
                new TextureAtlas(this, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhitePixel);
        }

        void IRenderer.Initialise()
        {
            string version = GL.GetString(StringName.Version);
            IsEmbedded = version.Contains("OpenGL ES"); // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml

            MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
            MaxRenderBufferSize = GL.GetInteger(GetPName.MaxRenderbufferSize);

            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);

            defaultQuadBatch = CreateQuadBatch<TexturedVertex2D>(100, 1000);

            resetScheduler.AddDelayed(checkPendingDisposals, 0, true);
            isInitialised = true;
        }

        private void checkPendingDisposals()
        {
            disposalQueue.CheckPendingDisposals();
        }

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
            lastBlendingEnabledState = null;

            foreach (var b in batchResetList)
                b.ResetCounters();
            batchResetList.Clear();

            currentShader?.Unbind();
            currentShader = null;
            shaderStack.Clear();
            GL.UseProgram(0);

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
            while (textureUploadQueue.TryDequeue(out ITexture? texture))
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

            lastBoundTexture.AsSpan().Clear();
            lastBoundTextureIsAtlas.AsSpan().Clear();
            lastBoundBuffers.AsSpan().Clear();
        }

        void IRenderer.FinishFrame()
        {
            flushCurrentBatch();
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
            PushDepthInfo(new DepthInfo(writeDepth: true));
            PushScissorState(false);
            if (clearInfo.Colour != currentClearInfo.Colour)
                GL.ClearColor(clearInfo.Colour);

            if (clearInfo.Depth != currentClearInfo.Depth)
            {
                if (IsEmbedded)
                {
                    // GL ES only supports glClearDepthf
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/es3.0/html/glClearDepthf.xhtml
                    GL.ClearDepth((float)clearInfo.Depth);
                }
                else
                {
                    // Older desktop platforms don't support glClearDepthf, so standard GL's double version is used instead
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glClearDepth.xhtml
                    osuTK.Graphics.OpenGL.GL.ClearDepth(clearInfo.Depth);
                }
            }

            if (clearInfo.Stencil != currentClearInfo.Stencil)
                GL.ClearStencil(clearInfo.Stencil);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            currentClearInfo = clearInfo;

            PopScissorState();
            PopDepthInfo();
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

            if (enabled)
                GL.Enable(EnableCap.ScissorTest);
            else
                GL.Disable(EnableCap.ScissorTest);
        }

        public bool BindBuffer(BufferTarget target, int buffer)
        {
            int bufferIndex = target - BufferTarget.ArrayBuffer;
            if (lastBoundBuffers[bufferIndex] == buffer)
                return false;

            lastBoundBuffers[bufferIndex] = buffer;
            GL.BindBuffer(target, buffer);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            return true;
        }

        public bool BindTexture(Texture texture, TextureUnit unit = TextureUnit.Texture0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
        {
            if (texture.TextureGL is TextureSubAtlasWhite && atlasTextureIsBound(unit))
            {
                // We can use the special white space from any atlas texture.
                return true;
            }

            bool didBind = texture.TextureGL.Bind(unit, wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);
            lastBoundTextureIsAtlas[getTextureUnitId(unit)] = texture.TextureGL is TextureGLAtlas;

            return didBind;
        }

        public bool BindTexture(int textureId, TextureUnit unit = TextureUnit.Texture0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            int index = getTextureUnitId(unit);

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

            if (lastActiveTextureUnit == unit && lastBoundTexture[index] == textureId)
                return false;

            flushCurrentBatch();

            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            lastBoundTexture[index] = textureId;
            lastBoundTextureIsAtlas[getTextureUnitId(unit)] = false;
            lastActiveTextureUnit = unit;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        private int getTextureUnitId(TextureUnit unit) => (int)unit - (int)TextureUnit.Texture0;
        private bool atlasTextureIsBound(TextureUnit unit) => lastBoundTextureIsAtlas[getTextureUnitId(unit)];

        public void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters == blendingParameters)
                return;

            flushCurrentBatch();

            if (blendingParameters.IsDisabled)
            {
                if (!lastBlendingEnabledState.HasValue || lastBlendingEnabledState.Value)
                    GL.Disable(EnableCap.Blend);

                lastBlendingEnabledState = false;
            }
            else
            {
                if (!lastBlendingEnabledState.HasValue || !lastBlendingEnabledState.Value)
                    GL.Enable(EnableCap.Blend);

                lastBlendingEnabledState = true;

                GL.BlendEquationSeparate(blendingParameters.RGBEquationMode, blendingParameters.AlphaEquationMode);
                GL.BlendFuncSeparate(blendingParameters.SourceBlendingFactor, blendingParameters.DestinationBlendingFactor,
                    blendingParameters.SourceAlphaBlendingFactor, blendingParameters.DestinationAlphaBlendingFactor);
            }

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

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
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

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
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

            GL.Scissor(scissor.X, Viewport.Height - scissor.Bottom, scissor.Width, scissor.Height);
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

            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(Ortho.Left, Ortho.Right, Ortho.Bottom, Ortho.Top, -1, 1);
            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);
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

            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(Ortho.Left, Ortho.Right, Ortho.Bottom, Ortho.Top, -1, 1);
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

            if (depthInfo.DepthTest)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(depthInfo.Function);
            }
            else
                GL.Disable(EnableCap.DepthTest);

            GL.DepthMask(depthInfo.WriteDepth);
        }

        public void BindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            bool alreadyBound = frameBufferStack.Count > 0 && frameBufferStack.Peek() == frameBuffer;

            frameBufferStack.Push(frameBuffer);

            if (!alreadyBound)
            {
                flushCurrentBatch();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);

                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UnbindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            if (frameBufferStack.Peek() != frameBuffer)
                return;

            frameBufferStack.Pop();

            flushCurrentBatch();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferStack.Peek());

            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UseProgram(Shader? shader)
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

            GL.UseProgram(shader);
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

        public void EnqueueTextureUpload(ITexture texture)
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
            => new FrameBuffer(this, renderBufferFormats, filteringMode);

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveType primitiveType) where TVertex : struct, IEquatable<TVertex>, IVertex
            => new LinearBatch<TVertex>(this, size, maxBuffers, primitiveType);

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : struct, IEquatable<TVertex>, IVertex
            => new QuadBatch<TVertex>(this, size, maxBuffers);

        public ITexture CreateTexture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None,
                                      Rgba32 initialisationColour = default)
            => new TextureGLSingle(this, width, height, manualMipmaps, filteringMode, wrapModeS, wrapModeT, initialisationColour);

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
            if (uniform.Owner == currentShader)
                flushCurrentBatch();

            switch (uniform)
            {
                case IUniformWithValue<bool> b:
                    GL.Uniform1(uniform.Location, b.GetValue() ? 1 : 0);
                    break;

                case IUniformWithValue<int> i:
                    GL.Uniform1(uniform.Location, i.GetValue());
                    break;

                case IUniformWithValue<float> f:
                    GL.Uniform1(uniform.Location, f.GetValue());
                    break;

                case IUniformWithValue<Vector2> v2:
                    GL.Uniform2(uniform.Location, ref v2.GetValueByRef());
                    break;

                case IUniformWithValue<Vector3> v3:
                    GL.Uniform3(uniform.Location, ref v3.GetValueByRef());
                    break;

                case IUniformWithValue<Vector4> v4:
                    GL.Uniform4(uniform.Location, ref v4.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix2> m2:
                    GL.UniformMatrix2(uniform.Location, false, ref m2.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix3> m3:
                    GL.UniformMatrix3(uniform.Location, false, ref m3.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix4> m4:
                    GL.UniformMatrix4(uniform.Location, false, ref m4.GetValueByRef());
                    break;
            }
        }

        void IRenderer.RegisterVertexBufferUse(IVertexBuffer buffer) => vertexBuffersInUse.Add(buffer);

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

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            while (frameBufferStack.Peek() == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(GL.DeleteFramebuffer, frameBuffer);
        }

        private void flushCurrentBatch()
        {
            lastActiveBatch?.Draw();
        }
    }
}
