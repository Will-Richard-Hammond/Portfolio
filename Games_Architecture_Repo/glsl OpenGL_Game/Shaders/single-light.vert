#version 330

layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec2 a_TexCoord;
layout (location = 2) in vec3 a_Normal;

uniform mat4 ModelViewProjMat;
uniform mat4 ModelMat;

out vec2 v_TexCoord;
out vec3 v_Normal;
out vec3 v_FragPos;

void main()
{
    // Position in clip space (MVP must be supplied consistently from C#)
    gl_Position = ModelViewProjMat * vec4(a_Position, 1.0);

    // World-space fragment position
    v_FragPos = vec3(ModelMat * vec4(a_Position, 1.0));
    v_TexCoord = a_TexCoord;

    // Correct normal transformation: use inverse-transpose of the model matrix (handle non-uniform scale)
    mat3 normalMatrix = transpose(inverse(mat3(ModelMat)));
    v_Normal = normalize(normalMatrix * a_Normal);
}