// This file is automatically included in every shader.

#ifndef INTERNAL_MASKING_INFO_H
#define INTERNAL_MASKING_INFO_H

#extension GL_ARB_shader_storage_buffer_object : enable

struct MaskingInfo
{
    mat4 ToMaskingSpace;
    mat4 ToScissorSpace;

    bool IsMasking;
    highp float CornerRadius;
    highp float CornerExponent;
    highp float BorderThickness;

    highp vec4 MaskingRect;
    highp vec4 ScissorRect;

    lowp mat4 BorderColour;
    mediump float MaskingBlendRange;
    lowp float AlphaExponent;
    highp vec2 EdgeOffset;

    bool DiscardInner;
    highp float InnerCornerRadius;
};

MaskingInfo g_MaskingInfo;

#ifndef OSU_GRAPHICS_NO_SSBO

layout(std140, set = -2, binding = 0) readonly buffer g_MaskingBuffer
{
    MaskingInfo Data[];
} MaskingBuffer;

void InitMasking(int index)
{
    g_MaskingInfo = MaskingBuffer.Data[index];
}

#else // OSU_GRAPHICS_NO_SSBO

layout(std140, set = -2, binding = 0) uniform g_MaskingBuffer
{
    MaskingInfo Data[64];
} MaskingBuffer;

void InitMasking(int index)
{
    g_MaskingInfo = MaskingBuffer.Data[index];
}

#endif // OSU_GRAPHICS_NO_SSBO
#endif // INTERNAL_MASKING_INFO_H