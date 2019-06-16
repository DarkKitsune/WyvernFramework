#version 450

layout (location = 0) out vec3 out_Color;
layout (location = 1) out vec2 out_TexCoord;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main() {
    vec2 pos[4] = vec2[3](vec2(-1, -1), vec2(1, -1), vec2(1, 1));
    vec3 color[4] = vec3[3](vec3(1.0, 0.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 0.0));
    vec2 texCoord[4] = vec2[3](vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(0.0, 1.1));

    out_Color = color[gl_VertexIndex];
    out_TexCoord = texCoord[gl_VertexIndex];
    gl_Position = vec4(pos[gl_VertexIndex], 0.0, 1.0);
}
