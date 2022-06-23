// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// The maximum allowed texture size.
        /// </summary>
        int MaxTextureSize { get; }

        /// <summary>
        /// The maximum allowed render buffer size.
        /// </summary>
        int MaxRenderBufferSize { get; }

        /// <summary>
        /// The maximum number of texture uploads to dequeue and upload per frame.
        /// Defaults to 32.
        /// </summary>
        int MaxTexturesUploadedPerFrame { get; set; }

        /// <summary>
        /// The maximum number of pixels to upload per frame.
        /// Defaults to 2 megapixels (8mb alloc).
        /// </summary>
        int MaxPixelsUploadedPerFrame { get; set; }

        /// <summary>
        /// Whether the current platform is embedded.
        /// </summary>
        bool IsEmbedded { get; }

        /// <summary>
        /// The current reset index.
        /// </summary>
        ulong ResetId { get; }

        /// <summary>
        /// The current masking parameters.
        /// </summary>
        ref readonly MaskingInfo CurrentMaskingInfo { get; }

        /// <summary>
        /// The current viewport.
        /// </summary>
        RectangleI Viewport { get; }

        /// <summary>
        /// The current orthographic projection rectangle.
        /// </summary>
        RectangleF Ortho { get; }

        /// <summary>
        /// The current scissor rectangle.
        /// </summary>
        RectangleI Scissor { get; }

        /// <summary>
        /// The current scissor offset.
        /// </summary>
        Vector2I ScissorOffset { get; }

        /// <summary>
        /// The current projection matrix.
        /// </summary>
        Matrix4 ProjectionMatrix { get; }

        /// <summary>
        /// The current depth parameters.
        /// </summary>
        DepthInfo CurrentDepthInfo { get; }

        /// <summary>
        /// The current horizontal texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeS { get; }

        /// <summary>
        /// The current vertical texture wrap mode.
        /// </summary>
        WrapMode CurrentWrapModeT { get; }

        /// <summary>
        /// Whether any masking parameters are currently applied.
        /// </summary>
        bool IsMaskingActive { get; }

        /// <summary>
        /// The current backbuffer depth.
        /// </summary>
        float BackbufferDrawDepth { get; }

        bool UsingBackbuffer { get; }

        /// <summary>
        /// Performs a once-off initialisation of this <see cref="IRenderer"/>.
        /// </summary>
        /// <param name="host">The <see cref="GameHost"/> using this <see cref="IRenderer"/>.</param>
        internal void Initialise(GameHost host);

        /// <summary>
        /// Resets any states to prepare for drawing a new frame.
        /// </summary>
        /// <param name="windowSize">The full window size.</param>
        internal void BeginFrame(Vector2 windowSize);

        /// <summary>
        /// Performs any last actions before a frame ends.
        /// </summary>
        internal void FinishFrame();

        /// <summary>
        /// Clears the currently bound frame buffer.
        /// </summary>
        /// <param name="clearInfo">The clearing parameters.</param>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Applies a new scissor test enablement state.
        /// </summary>
        /// <param name="enabled">Whether the scissor test is enabled.</param>
        void PushScissorState(bool enabled);

        /// <summary>
        /// Restores the last scissor test enablement state.
        /// </summary>
        void PopScissorState();

        /// <summary>
        /// Binds a vertex buffer.
        /// </summary>
        /// <param name="target">The target to bind the buffer to.</param>
        /// <param name="buffer">The buffer to bind.</param>
        /// <returns>Whether <paramref name="buffer"/> was newly-bound.</returns>
        bool BindBuffer(BufferTarget target, int buffer);

        /// <summary>
        /// Binds a texture.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The unit to bind the texture to.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether <paramref name="texture"/> was newly-bound.</returns>
        bool BindTexture(TextureGL? texture, TextureUnit unit = TextureUnit.Texture0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None);

        /// <summary>
        /// Binds a texture.
        /// </summary>
        /// <param name="textureId">The texture to bind.</param>
        /// <param name="unit">The unit to bind the texture to.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether <paramref name="textureId"/> was newly-bound.</returns>
        bool BindTexture(int textureId, TextureUnit unit = TextureUnit.Texture0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None);

        /// <summary>
        /// Sets the current blending state.
        /// </summary>
        /// <param name="blendingParameters">The blending parameters.</param>
        void SetBlend(BlendingParameters blendingParameters);

        /// <summary>
        /// Applies a new viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        void PushViewport(RectangleI viewport);

        /// <summary>
        /// Restores the last viewport rectangle.
        /// </summary>
        void PopViewport();

        /// <summary>
        /// Applies a new scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle.</param>
        void PushScissor(RectangleI scissor);

        /// <summary>
        /// Restores the last scissor rectangle.
        /// </summary>
        void PopScissor();

        /// <summary>
        /// Applies a new scissor offset to the scissor rectangle.
        /// </summary>
        /// <param name="offset">The scissor offset.</param>
        void PushScissorOffset(Vector2I offset);

        /// <summary>
        /// Restores the last scissor offset.
        /// </summary>
        void PopScissorOffset();

        /// <summary>
        /// Applies a new orthographic projection rectangle.
        /// </summary>
        /// <param name="ortho">The rectangle to create the orthographic projection from.</param>
        void PushOrtho(RectangleF ortho);

        /// <summary>
        /// Restores the last orthographic projection rectangle.
        /// </summary>
        void PopOrtho();

        /// <summary>
        /// Applies new masking parameters.
        /// </summary>
        /// <param name="maskingInfo">The masking parameters.</param>
        /// <param name="overwritePreviousScissor">Whether to use the last scissor rectangle.</param>
        void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false);

        /// <summary>
        /// Restores the last masking parameters.
        /// </summary>
        void PopMaskingInfo();

        /// <summary>
        /// Applies new depth parameters.
        /// </summary>
        /// <param name="depthInfo">The depth parameters.</param>
        void PushDepthInfo(DepthInfo depthInfo);

        /// <summary>
        /// Restores the last depth parameters.
        /// </summary>
        void PopDepthInfo();

        /// <summary>
        /// Binds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to bind.</param>
        void BindFrameBuffer(int frameBuffer);

        /// <summary>
        /// Unbinds a framebuffer, if bound.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to unbind.</param>
        void UnbindFrameBuffer(int frameBuffer);

        /// <summary>
        /// Binds a shader.
        /// </summary>
        /// <param name="shader">The shader to bind.</param>
        void UseProgram(Shader? shader);

        /// <summary>
        /// Notifies that a <see cref="IVertexBuffer"/> has begun being used.
        /// </summary>
        /// <param name="buffer">The <see cref="IVertexBuffer"/> in use.</param>
        internal void RegisterVertexBufferUse(IVertexBuffer buffer);

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change GL state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        internal void SetActiveBatch(IVertexBatch batch);

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="VertexBuffer{T}"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        internal void SetDrawDepth(float drawDepth);
    }
}
