#version 330 core
struct Material {
    sampler2D diffuse;
};

out vec4 FragColor;

//In order to calculate some basic lighting we need a few things per model basis, and a few things per fragment basis:
uniform vec3 objectColor; //The color of the object.
uniform vec3 lightColor; //The color of the light.
uniform vec3 lightColor2; //The color of the light.
uniform vec3 lightDir; //The position of the light.
uniform vec3 lightDir2; //The position of the light.
uniform Material material;

in vec3 Normal; //The normal of the fragment is calculated in the vertex shader.
in vec3 FragPos; //The fragment position.
in vec2 TexCoords;

void main()
{
    float ambientStrength = 0.25;
    vec3 ambient = ambientStrength * lightColor * vec3(texture(material.diffuse, TexCoords));

    //We calculate the light direction, and make sure the normal is normalized.
    vec3 norm = normalize(Normal);
	
    float diff = max(dot(norm, lightDir), 0.0); //We make sure the value is non negative with the max function.
    vec3 diffuse = diff * lightColor * vec3(texture(material.diffuse, TexCoords));
	
    float diff2 = max(dot(norm, lightDir2), 0.0); //We make sure the value is non negative with the max function.
    vec3 diffuse2 = diff2 * lightColor2 * vec3(texture(material.diffuse, TexCoords));
	
    vec3 result = (ambient + diffuse + diffuse2) * objectColor;
    FragColor = vec4(result, 1.0);
}