#include "sh_Utils.h"

in highp vec2 m_Position;
in lowp vec4 m_Colour;
in mediump vec2 m_TexCoord;
in mediump vec4 m_TexRect;
in mediump vec2 m_BlendRange;

out highp vec2 v_MaskingPosition;
out lowp vec4 v_Colour;
out mediump vec2 v_TexCoord;
out mediump vec4 v_TexRect;
out mediump vec2 v_BlendRange;

uniform highp mat4 g_ProjMatrix;
uniform highp mat3 g_ToMaskingSpace;

void main(void)
{
	// Transform from screen space to masking space.
	highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;

	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}
