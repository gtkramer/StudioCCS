using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace StudioCCS
{
    /// <summary>
    /// Description of TestTriangle.
    /// </summary>
    public class TestTriangle
    {
        private int ProgramID;
        private int AttribPos;
        private int AttribColor;
        private int UniMatrix;

        private int VAOIndex = -1;
        private int VBOIndex = -1;

        public Vector3 Position = new Vector3();

        public bool WasInit = false;

        [StructLayout(LayoutKind.Sequential)]
        public struct TriangleVertex
        {
            public Vector3 Position;
            public Vector3 Color;
        }

        private TriangleVertex[] Vertices;


        public TestTriangle()
        {
            Vertices = new TriangleVertex[3];
            //Vertices[0].Position = new Vector3(-0.8f, -0.8f, 0.0f);
            Vertices[0].Position = new Vector3(0.374f, 2.792f, 0.186f);
            Vertices[0].Color = new Vector3(1.0f, 0.0f, 0.0f);

            //Vertices[1].Position = new Vector3(0.8f, -0.8f, 0.0f);
            Vertices[1].Position = new Vector3(0.28f, 2.792f, 0.186f);
            Vertices[1].Color = new Vector3(0.0f, 1.0f, 0.0f);

            //Vertices[2].Position = new Vector3(0.0f, 0.8f, 0.0f);
            Vertices[2].Position = new Vector3(0.28f, 2.792f, 0.411f);
            Vertices[2].Color = new Vector3(0.0f, 0.0f, 1.0f);
        }

        public bool Init()
        {
            ProgramID = Scene.LoadProgram("Triangle");
            if (ProgramID != -1)
            {
                AttribPos = GL.GetAttribLocation(ProgramID, "vPosition");
                AttribColor = GL.GetAttribLocation(ProgramID, "vColor");
                UniMatrix = GL.GetUniformLocation(ProgramID, "modelView");

                if (AttribPos == -1 || AttribColor == -1 || UniMatrix == -1)
                {
                    Debug.WriteLine("TestTriangle::Init(): Error binding Vertex Attribs:");
                    Debug.WriteLine(string.Format("\taPos: {0}, aColor: {1}, uMatrix: {2}", AttribPos, AttribColor, UniMatrix));
                    return false;
                }

                VAOIndex = GL.GenVertexArray();
                GL.BindVertexArray(VAOIndex);

                int VertexSize = Marshal.SizeOf(Vertices[0].GetType());
                Type VertexType = Vertices[0].GetType();

                VBOIndex = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOIndex);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexSize * 3), Vertices, BufferUsageHint.StaticDraw);

                GL.EnableVertexAttribArray(AttribPos);
                GL.VertexAttribPointer(AttribPos, 3, VertexAttribPointerType.Float, false, VertexSize, Marshal.OffsetOf(VertexType, "Position"));

                GL.EnableVertexAttribArray(AttribColor);
                GL.VertexAttribPointer(AttribColor, 3, VertexAttribPointerType.Float, false, VertexSize, Marshal.OffsetOf(VertexType, "Color"));


                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                WasInit = true;
            }
            return WasInit;
        }

        public void DeInit()
        {
            if (ProgramID != -1)
            {
                GL.DeleteProgram(ProgramID);
            }

            ProgramID = -1;

            if (VBOIndex != -1)
            {
                GL.DeleteBuffer(VBOIndex);
            }

            VBOIndex = -1;
            if (VAOIndex != -1)
            {
                GL.DeleteVertexArray(VAOIndex);
            }

            VAOIndex = -1;
        }

        public void Render(Matrix4 ViewMtx)
        {
            if (WasInit)
            {
                Matrix4 modelViewMtx = Matrix4.CreateTranslation(-Position) * ViewMtx;
                GL.UseProgram(ProgramID);

                GL.UniformMatrix4(UniMatrix, false, ref modelViewMtx);

                GL.BindVertexArray(VAOIndex);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOIndex);

                GL.EnableVertexAttribArray(AttribPos);
                GL.EnableVertexAttribArray(AttribColor);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

                GL.DisableVertexAttribArray(AttribPos);
                GL.DisableVertexAttribArray(AttribColor);

                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.UseProgram(0);
            }
        }
    }
}
