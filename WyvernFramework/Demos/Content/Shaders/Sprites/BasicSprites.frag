#version 450

layout (location = 0) in vec3 in_Color;
layout (location = 1) in vec2 in_TexCoord;
layout (location = 0) out vec4 out_Color;

layout (binding = 0) uniform sampler2D diffuse;

void main() {
    out_Color = vec4(in_Color, 1.0) * texture(diffuse, in_TexCoord);
}
