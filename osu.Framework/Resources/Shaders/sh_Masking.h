layout(location = 0) in highp vec2 v_MaskingPosition;
layout(location = 1) in lowp vec4 v_Colour;

#ifdef HIGH_PRECISION_VERTEX
	layout(location = 3) in highp vec4 v_TexRect;
#else
	layout(location = 3) in mediump vec4 v_TexRect;
#endif

layout(location = 4) in mediump vec2 v_BlendRange;
layout(location = 5) flat in int v_MaskingIndex;

MaskingInfo maskingInfo;

highp float distanceFromRoundedRect(highp vec2 offset, highp float radius)
{
	highp vec2 maskingPosition = v_MaskingPosition + offset;

	// Compute offset distance from masking rect in masking space.
	highp vec2 topLeftOffset = maskingInfo.MaskingRect.xy - maskingPosition;
	highp vec2 bottomRightOffset = maskingPosition - maskingInfo.MaskingRect.zw;

	highp vec2 distanceFromShrunkRect = max(
		bottomRightOffset + vec2(radius),
		topLeftOffset + vec2(radius));

	highp float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

	// Inside the shrunk rectangle
	if (maxDist <= 0.0)
		return maxDist;
	// Outside of the shrunk rectangle
	else
	{
		distanceFromShrunkRect = max(vec2(0.0), distanceFromShrunkRect);
		return pow(pow(distanceFromShrunkRect.x, maskingInfo.CornerExponent) + pow(distanceFromShrunkRect.y, maskingInfo.CornerExponent), 1.0 / maskingInfo.CornerExponent);
	}
}

highp float distanceFromDrawingRect(mediump vec2 texCoord)
{
	highp vec2 topLeftOffset = v_TexRect.xy - texCoord;
	topLeftOffset = vec2(
		v_BlendRange.x > 0.0 ? topLeftOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? topLeftOffset.y / v_BlendRange.y : 0.0);

	highp vec2 bottomRightOffset = texCoord - v_TexRect.zw;
	bottomRightOffset = vec2(
		v_BlendRange.x > 0.0 ? bottomRightOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? bottomRightOffset.y / v_BlendRange.y : 0.0);

	highp vec2 xyDistance = max(topLeftOffset, bottomRightOffset);
	return max(xyDistance.x, xyDistance.y);
}

lowp vec4 getBorderColour()
{
    highp vec2 relativeTexCoord = v_MaskingPosition / (maskingInfo.MaskingRect.zw - maskingInfo.MaskingRect.xy);
    lowp vec4 top = mix(maskingInfo.BorderColour[0], maskingInfo.BorderColour[2], relativeTexCoord.x);
    lowp vec4 bottom = mix(maskingInfo.BorderColour[1], maskingInfo.BorderColour[3], relativeTexCoord.x);
    return mix(top, bottom, relativeTexCoord.y);
}

lowp vec4 getRoundedColor(lowp vec4 texel, mediump vec2 texCoord)
{
	maskingInfo = GetMaskingInfo(v_MaskingIndex);

	if (!maskingInfo.IsMasking && v_BlendRange == vec2(0.0))
	{
		return v_Colour * texel;
	}

	highp float dist = distanceFromRoundedRect(vec2(0.0), maskingInfo.CornerRadius);
	lowp float alphaFactor = 1.0;

	// Discard inner pixels
	if (maskingInfo.DiscardInner)
	{
		highp float innerDist = (maskingInfo.EdgeOffset == vec2(0.0) && maskingInfo.InnerCornerRadius == maskingInfo.CornerRadius) ?
			dist : distanceFromRoundedRect(maskingInfo.EdgeOffset, maskingInfo.InnerCornerRadius);

		// v_BlendRange is set from outside in a hacky way to tell us the maskingInfo.MaskingBlendRange used for the rounded
		// corners of the edge effect container itself. We can then derive the alpha factor for smooth inner edge
		// effect from that.
		highp float innerBlendFactor = (maskingInfo.InnerCornerRadius - maskingInfo.MaskingBlendRange - innerDist) / v_BlendRange.x;
		if (innerBlendFactor > 1.0)
		{
			return vec4(0.0);
		}

		// We exponentiate our factor to exactly counteract the later exponentiation by maskingInfo.AlphaExponent for a smoother inner border.
		alphaFactor = pow(min(1.0 - innerBlendFactor, 1.0), 1.0 / maskingInfo.AlphaExponent);
	}

	dist /= maskingInfo.MaskingBlendRange;

	// This correction is needed to avoid fading of the alpha value for radii below 1px.
	highp float radiusCorrection = maskingInfo.CornerRadius <= 0.0 ? maskingInfo.MaskingBlendRange : max(0.0, maskingInfo.MaskingBlendRange - maskingInfo.CornerRadius);
	highp float fadeStart = (maskingInfo.CornerRadius + radiusCorrection) / maskingInfo.MaskingBlendRange;
	alphaFactor *= min(fadeStart - dist, 1.0);

	if (v_BlendRange.x > 0.0 || v_BlendRange.y > 0.0)
	{
		alphaFactor *= clamp(1.0 - distanceFromDrawingRect(texCoord), 0.0, 1.0);
	}

	if (alphaFactor <= 0.0)
	{
		return vec4(0.0);
	}

	// This ends up softening glow without negatively affecting edge smoothness much.
	alphaFactor = pow(alphaFactor, maskingInfo.AlphaExponent);

	highp float borderStart = 1.0 + fadeStart - maskingInfo.BorderThickness;
	lowp float colourWeight = min(borderStart - dist, 1.0);

	lowp vec4 borderColour = getBorderColour();

	if (colourWeight <= 0.0)
	{
		return vec4(borderColour.rgb, borderColour.a * alphaFactor);
	}

	lowp vec4 dest = vec4(v_Colour.rgb, v_Colour.a * alphaFactor) * texel;
	lowp vec4 src = vec4(borderColour.rgb, borderColour.a * (1.0 - colourWeight));

	return blend(src, dest);
}