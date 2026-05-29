#version 330 core
in vec4 Vertex_Color;
out vec4 FragColor;

void main(void)
{
	FragColor = Vertex_Color;
}