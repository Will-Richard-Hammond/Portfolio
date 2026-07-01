#version 330

in vec2 v_TexCoord;

uniform sampler2D s_texture;

out vec4 Color;

void main()
{
    // Directly sample the texture with no lighting applied
    vec4 tex = texture(s_texture, v_TexCoord);
    // If texture has premultiplied alpha or you want full brightness, you can adjust here.
    Color = tex;
}