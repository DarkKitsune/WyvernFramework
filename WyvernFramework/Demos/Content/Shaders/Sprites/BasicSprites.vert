#version 450

const int MAX_LISTS = 16;

layout (location = 0) in float in_InstanceTime;
layout (location = 1) in vec3 in_InstancePosition;
layout (location = 2) in vec3 in_InstanceVelocity;
layout (location = 3) in vec2 in_InstanceScale;
layout (location = 4) in int in_InstanceListIndex;
layout (location = 5) in vec4 in_InstanceRectangle;
layout (location = 0) out vec2 out_TexCoord;

layout (binding = 0) uniform struct_Camera
{
	mat4 view;
	mat4 projection;
} block_Camera;

layout (binding = 2) uniform struct_Time
{
	float listTime[MAX_LISTS];
} time;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    vec2 pos[4] = vec2[4](vec2(-0.5, 0.5), vec2(0.5, 0.5), vec2(0.5, -0.5), vec2(-0.5, -0.5));
    vec2 texCoord[4] = vec2[4](vec2(0, 0), vec2(1, 0), vec2(1, 1), vec2(0, 1));
    int index[6] = int[6](0, 1, 2, 2, 3, 0);

    out_TexCoord = in_InstanceRectangle.xy + in_InstanceRectangle.zw * texCoord[index[gl_VertexIndex]];
    float timePassed = time.listTime[in_InstanceListIndex] - in_InstanceTime;
    vec3 iPos = in_InstancePosition + in_InstanceVelocity * timePassed;
    gl_Position = block_Camera.projection * block_Camera.view * vec4(iPos.xy + pos[index[gl_VertexIndex]] * in_InstanceScale, iPos.z, 1);
}
