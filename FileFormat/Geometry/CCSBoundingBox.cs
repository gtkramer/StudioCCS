using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StudioCCS.Logging;
using StudioCCS.Rendering;
namespace StudioCCS.FileFormat.Geometry
{
    public class CCSBoundingBox : CCSBaseObject
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct BBox
        {
            public Vector3 Minimum;// = Vector3.Zero;
            public Vector3 Maximum; // = Vector3.Zero;
            public Vector4 Color; // = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        }

        public int ModelID = 0;
        public BBox[] Box = new BBox[1];

        //OpenGL Stuff
        public static int ProgramID = -1;
        public static int ProgramRefs = 0;
        public static int AttribVMin = -1;
        public static int AttribVMax = -1;
        public static int AttribVColor = -1;
        public static int UniMatrix = -1;

        public int VertexArrayID = -1;
        public int VertexBufferID = -1;

        public CCSBoundingBox(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_BBOX;
        }

        public override bool Init()
        {
            if (ProgramID == -1)
            {
                ProgramID = Scene.LoadProgram("BoundingBox", true);
                if (ProgramID == -1)
                {
                    return false;
                }

                AttribVMin = GL.GetAttribLocation(ProgramID, "VMin");
                AttribVMax = GL.GetAttribLocation(ProgramID, "VMax");
                AttribVColor = GL.GetAttribLocation(ProgramID, "VColor");
                UniMatrix = GL.GetUniformLocation(ProgramID, "UMatrix");

                if (AttribVMin == -1 || AttribVMax == -1 || AttribVColor == -1 || UniMatrix == -1)
                {
                    Log.Error("CCSBBox: Error Getting Shader Attributes/Uniforms:\n");
                    Log.Error(string.Format("\tVMin: {0}, VMax: {1}, VColor: {2}, UMatrix: {3}", AttribVMin, AttribVMax, AttribVColor, UniMatrix));
                    return false;
                }
            }

            ProgramRefs += 1;
            // Read() already populated Box[0].Minimum/Maximum; only the wireframe
            // colour is left to set here (a fresh BBox would wipe the extents).
            Box[0].Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            VertexArrayID = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayID);

            VertexBufferID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);

            Type VertexType = Box[0].GetType();
            int VertexSize = Marshal.SizeOf(VertexType);

            GL.BufferData(BufferTarget.ArrayBuffer, VertexSize, Box, BufferUsageHint.DynamicDraw);

            GL.EnableVertexAttribArray(AttribVMin);
            GL.VertexAttribPointer(AttribVMin, 3, VertexAttribPointerType.Float, false, VertexSize, Marshal.OffsetOf(VertexType, "Minimum"));

            GL.EnableVertexAttribArray(AttribVMax);
            GL.VertexAttribPointer(AttribVMax, 3, VertexAttribPointerType.Float, false, VertexSize, Marshal.OffsetOf(VertexType, "Maximum"));

            GL.EnableVertexAttribArray(AttribVColor);
            GL.VertexAttribPointer(AttribVColor, 4, VertexAttribPointerType.Float, false, VertexSize, Marshal.OffsetOf(VertexType, "Color"));

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            return true;
        }

        public void Render(Matrix4 ProjViewMtx)
        {
            if (ProgramID == -1 || VertexArrayID == -1)
            {
                return;
            }

            GL.UseProgram(ProgramID);
            GL.BindVertexArray(VertexArrayID);

            GL.UniformMatrix4(UniMatrix, false, ref ProjViewMtx);

            // One point per box; the geometry shader expands it into the 12 edges.
            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public override bool DeInit()
        {
            if (VertexArrayID != -1)
            {
                GL.DeleteVertexArray(VertexArrayID);
            }

            if (VertexBufferID != -1)
            {
                GL.DeleteBuffer(VertexBufferID);
            }

            ProgramRefs -= 1;
            if (ProgramRefs <= 0)
            {
                if (ProgramID != -1)
                {
                    GL.DeleteProgram(ProgramID);
                }

                ProgramID = -1;
            }

            return true;
        }

        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            ModelID = bStream.ReadInt32();
            Box[0].Minimum = Util.ReadVec3Position(bStream);
            Box[0].Maximum = Util.ReadVec3Position(bStream);

            return true;
        }

        public override CCSTreeNode ToNode()
        {
            return base.ToNode();
        }
    }
}
