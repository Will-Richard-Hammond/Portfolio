#version 330

layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec2 a_TexCoord;

uniform mat4 ModelViewProjMat;
uniform mat4 ModelMat; // kept for compatibility; not used for lighting here

out vec2 v_TexCoord;

void main()
{
    // Position in clip space (ModelViewProjMat is computed by C# with view translation removed)
    gl_Position = ModelViewProjMat * vec4(a_Position, 1.0);

    // Pass through texture coordinates
    v_TexCoord = a_TexCoord;
}