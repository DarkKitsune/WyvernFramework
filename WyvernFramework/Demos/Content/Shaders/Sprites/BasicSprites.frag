#version 450

layout (location = 0) in vec2 in_TexCoord;
layout (location = 0) out vec4 out_Color;

layout (bstd140, inding = 1) uniform sampler2D sampler_Texture;

void main()
{
    out_Color = texture(sampler_Texture, in_TexCoord);
}
