#include "sh_Utils.h"

in mediump vec2 v_TexCoord;
in mediump vec4 v_TexRect;

out lowp vec4 f_Colour;

void main(void)
{
    float hueValue = v_TexCoord.x / (v_TexRect[2] - v_TexRect[0]);
    f_Colour = hsv2rgb(vec4(hueValue, 1, 1, 1));
}
