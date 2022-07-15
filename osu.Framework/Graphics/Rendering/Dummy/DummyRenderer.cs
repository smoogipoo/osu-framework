// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    public class DummyRenderer : IRenderer
    {
        public int MaxTextureSize => 1;
        public int MaxRenderBufferSize => 1;
        public int MaxTexturesUploadedPerFrame { get; set; }
        public int MaxPixelsUploadedPerFrame { get; set; }
        public bool IsEmbedded => false;
        public ulong ResetId => 0;

        public ref readonly MaskingInfo CurrentMaskingInfo => ref maskingInfo;

        private readonly MaskingInfo maskingInfo;

        public RectangleI Viewport => RectangleI.Empty;
        public RectangleF Ortho => RectangleF.Empty;
        public RectangleI Scissor => RectangleI.Empty;
        public Vector2I ScissorOffset => Vector2I.Zero;
        public Matrix4 ProjectionMatrix => Matrix4.Zero;
        public DepthInfo CurrentDepthInfo => DepthInfo.Default;
        public WrapMode CurrentWrapModeS => WrapMode.None;
        public WrapMode CurrentWrapModeT => WrapMode.None;
        public bool IsMaskingActive => false;
        public float BackbufferDrawDepth => 0;
        public bool UsingBackbuffer => false;
        public Texture WhitePixel { get; } = new Texture(new DummyNativeTexture(), WrapMode.None, WrapMode.None);

        public DummyRenderer()
        {
            maskingInfo = default;
        }

        void IRenderer.Initialise()
        {
        }

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
        }

        void IRenderer.FinishFrame()
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

        public bool BindBuffer(BufferTarget target, int buffer)
            => true;

        public bool BindTexture(Texture texture, TextureUnit unit = TextureUnit.Texture0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
            => true;

        public void SetBlend(BlendingParameters blendingParameters)
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

        public void PushOrtho(RectangleF ortho)
        {
        }

        public void PopOrtho()
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

        public void UseProgram(Shader? shader)
        {
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation)
        {
        }

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
        }

        public void EnqueueTextureUpload(INativeTexture texture)
        {
        }

        public IFrameBuffer CreateFrameBuffer(RenderbufferInternalFormat[]? renderBufferFormats = null, All filteringMode = All.Linear)
            => new DummyFrameBuffer();

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveType primitiveType)
            where TVertex : struct, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            where TVertex : struct, IEquatable<TVertex>, IVertex
            => new DummyVertexBatch<TVertex>();

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None,
                                     Rgba32 initialisationColour = default)
            => new Texture(new DummyNativeTexture { Width = width, Height = height }, wrapModeS, wrapModeT);

        public Texture CreateVideoTexture(int width, int height)
            => new Texture(new DummyNativeTexture { Width = width, Height = height }, WrapMode.None, WrapMode.None);

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
        }

        void IRenderer.RegisterVertexBufferUse(IVertexBuffer buffer)
        {
        }

        void IRenderer.SetActiveBatch(IVertexBatch batch)
        {
        }

        void IRenderer.SetDrawDepth(float drawDepth)
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
