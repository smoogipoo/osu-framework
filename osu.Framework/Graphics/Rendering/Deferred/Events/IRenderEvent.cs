// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal interface IRenderEvent
    {
        RenderEventType Type { get; }
    }

    internal enum RenderEventType : byte
    {
        AddPrimitiveToBatch,
        BindFrameBuffer,
        UnbindFrameBuffer,
        SetShader,
        BindTexture,
        BindUniformBlock,
        Clear,
        SetDepthInfo,
        SetScissor,
        SetScissorState,
        SetStencilInfo,
        SetViewport,
        SetBlend,
        SetBlendMask,
        SetUniformBufferData,
        Flush,
    }
}
