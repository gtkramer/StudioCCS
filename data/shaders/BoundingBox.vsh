#version 330 core

// One point per box; the geometry shader expands it into a wireframe AABB.
// Attribute names are bound by GetAttribLocation in CCSBoundingBox.cs, so they
// must match VMin/VMax/VColor exactly.
in vec3 VMin;
in vec3 VMax;
in vec4 VColor;

out BoundingBoxData
{
    vec3 bMin;
    vec3 bMax;
    vec4 color;
} box;

void main()
{
    gl_Position = vec4(0.0, 0.0, 0.0, 1.0);
    box.bMin = VMin;
    box.bMax = VMax;
    box.color = VColor;
}
