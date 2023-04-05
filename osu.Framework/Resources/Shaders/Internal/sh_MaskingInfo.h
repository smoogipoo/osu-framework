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