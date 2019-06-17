#version 450

layout (location = 0) in vec3 in_InstancePosition;
layout (location = 1) in vec2 in_InstanceScale;
layout (location = 0) out vec2 out_TexCoord;

layout (binding = 0) uniform struct_Camera
{
	mat4 view;
	mat4 projection;
} block_Camera;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    vec2 pos[4] = vec2[4](vec2(-0.5, 0.5), vec2(0.5, 0.5), vec2(0.5, -0.5), vec2(-0.5, -0.5));
    vec2 texCoord[4] = vec2[4](vec2(0, 0), vec2(1, 0), vec2(1, 1), vec2(0, 1));
    int index[6] = int[6](0, 1, 2, 2, 3, 0);

    out_TexCoord = texCoord[index[gl_VertexIndex]];
    gl_Position = block_Camera.projection * block_Camera.view * vec4(in_InstancePosition.xy + pos[index[gl_VertexIndex]] * in_InstanceScale, in_InstancePosition.z, 1);
}
