#version 330 core

// Expand a single point carrying an AABB (min/max corners) into the 12 edges of
// a wireframe box. Emitted as line strips: the two y-faces as closed 4-edge
// loops plus the 4 vertical edges as separate 2-vertex strips.
//   bottom loop (5) + top loop (5) + 4 verticals (2 each) = 18 vertices.
layout(points) in;
layout(line_strip, max_vertices = 18) out;

in BoundingBoxData
{
    vec3 bMin;
    vec3 bMax;
    vec4 color;
} box[];

uniform mat4 UMatrix;

out vec4 fColor;

void emit(vec3 p)
{
    fColor = box[0].color;
    gl_Position = UMatrix * vec4(p, 1.0);
    EmitVertex();
}

void main()
{
    vec3 m = box[0].bMin;
    vec3 M = box[0].bMax;

    // Corners: lower face (y = m.y) then upper face (y = M.y).
    vec3 c0 = vec3(m.x, m.y, m.z);
    vec3 c1 = vec3(M.x, m.y, m.z);
    vec3 c2 = vec3(M.x, m.y, M.z);
    vec3 c3 = vec3(m.x, m.y, M.z);
    vec3 c4 = vec3(m.x, M.y, m.z);
    vec3 c5 = vec3(M.x, M.y, m.z);
    vec3 c6 = vec3(M.x, M.y, M.z);
    vec3 c7 = vec3(m.x, M.y, M.z);

    // Lower face loop.
    emit(c0); emit(c1); emit(c2); emit(c3); emit(c0);
    EndPrimitive();

    // Upper face loop.
    emit(c4); emit(c5); emit(c6); emit(c7); emit(c4);
    EndPrimitive();

    // Vertical edges.
    emit(c0); emit(c4); EndPrimitive();
    emit(c1); emit(c5); EndPrimitive();
    emit(c2); emit(c6); EndPrimitive();
    emit(c3); emit(c7); EndPrimitive();
}
