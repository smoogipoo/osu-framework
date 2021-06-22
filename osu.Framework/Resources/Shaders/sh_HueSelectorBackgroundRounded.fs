#include "sh_Utils.h"
#include "sh_Masking.h"

in mediump vec2 v_TexCoord;

out lowp vec4 f_Colour;

void main(void)
{
    float hueValue = v_TexCoord.x / (v_TexRect[2] - v_TexRect[0]);
    f_Colour = getRoundedColor(hsv2rgb(vec4(hueValue, 1, 1, 1)), v_TexCoord);
}
