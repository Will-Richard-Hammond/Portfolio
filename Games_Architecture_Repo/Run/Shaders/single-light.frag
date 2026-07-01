#version 330
 
in vec2 v_TexCoord;
in vec3 v_Normal;
in vec3 v_FragPos;

uniform sampler2D s_texture;
uniform vec3 v_diffuse;	// OBJ NEW

out vec4 Color;
 
void main()
{
	vec3 lightColor = vec3(1,1,1);
	vec4 lightAmbient = vec4(0.1, 0.1, 0.1, 1.0);
	vec3 lightPos = vec3(0,10,20);

	vec3 norm = normalize(v_Normal);
	vec3 lightDir = normalize(lightPos - v_FragPos); 
	float diff = max(dot(norm, lightDir), 0.0);
	vec3 diffuse = diff * lightColor;

	Color = lightAmbient + (vec4(v_diffuse, 1) * texture(s_texture, v_TexCoord) * vec4(diffuse, 0));  // OBJ CHANGED
}