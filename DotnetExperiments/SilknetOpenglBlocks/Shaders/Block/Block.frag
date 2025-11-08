#version 330 core

in vec2 textureCoordinates;
in float light;
out vec4 color;
uniform sampler2D textureSampler;

void main()
{
    color = texture(textureSampler, textureCoordinates);
    color.rgb *= light;
}