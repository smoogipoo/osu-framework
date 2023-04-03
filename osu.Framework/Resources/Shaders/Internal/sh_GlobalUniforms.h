// This file is automatically included in every shader.

struct MaskingInfo
{
    mat3 ToMaskingSpace;

    bool IsMasking;
    highp float CornerRadius;
    highp float CornerExponent;
    highp vec4 MaskingRect;
    highp float BorderThickness;
    lowp mat4 BorderColour;
    mediump float MaskingBlendRange;
    lowp float AlphaExponent;
    highp vec2 EdgeOffset;
    bool DiscardInner;
    highp float InnerCornerRadius;
};

layout(std140, set = -2, binding = 0) uniform g_GlobalUniforms
{
    // Whether the backbuffer is currently being drawn to.
    bool g_BackbufferDraw;

    // Whether the depth values range from 0 to 1. If false, depth values range from -1 to 1.
    // OpenGL uses [-1, 1], Vulkan/D3D/MTL all use [0, 1].
    bool g_IsDepthRangeZeroToOne;

    // Whether the clip space ranges from -1 (top) to 1 (bottom). If false, the clip space ranges from -1 (bottom) to 1 (top).
    bool g_IsClipSpaceYInverted;

    // Whether the texture coordinates begin in the top-left of the texture. If false, (0, 0) is the bottom-left texel of the texture.
    bool g_IsUvOriginTopLeft;

    mat4 g_ProjMatrix;

    // 0 -> None
    // 1 -> ClampToEdge
    // 2 -> ClampToBorder
    // 3 -> Repeat
    int g_WrapModeS;
    int g_WrapModeT;
};

layout(std140, set = -1, binding = 0) readonly buffer g_MaskingBuffer
{
    MaskingInfo Data[];
} MaskingBuffer;

MaskingInfo GetMaskingInfo(int index)
{
    return MaskingBuffer.Data[index];
}

MaskingInfo g_MaskingInfo;