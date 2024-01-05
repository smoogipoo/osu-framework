// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public interface IRenderEvent
    {
        RenderEventType Type { get; }

        void Run(DeferredRenderer current, IRenderer target);
    }

    public enum RenderEventType
    {
        AddVertexToBatch,
        BindFrameBuffer,
        BindShader,
        BindTexture,
        BindUniformBlock,
        Clear,
        Disposal,
        DrawVertexBatch,
        ExpensiveOperation,
        PopDepthInfo,
        PopMaskingInfo,
        PopProjectionMatrix,
        PopQuadBatch,
        PopScissor,
        PopScissorOffset,
        PopScissorState,
        PopStencilInfo,
        PopViewport,
        PushDepthInfo,
        PushMaskingInfo,
        PushProjectionMatrix,
        PushQuadBatch,
        PushScissor,
        PushScissorOffset,
        PushScissorState,
        PushStencilInfo,
        PushViewport,
        SetBlend,
        SetBlendMask,
        SetUniformBufferData,
        UnbindFrameBuffer,
        UnbindShader
    }
}
