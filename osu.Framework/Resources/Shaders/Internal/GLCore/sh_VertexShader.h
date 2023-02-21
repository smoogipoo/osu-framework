// Automatically included for every vertex shader.

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord)
{
    return texture(samplerName, coord);
}