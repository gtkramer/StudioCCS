using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace StudioCCS.FileFormat.Raw
{
    public class CCSShadow : CCSBaseObject
    {
        public byte[] Data;
        public int DataSize;

        public CCSShadow(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_SHADOW;
        }

        public override bool Init()
        {
            //Currently nothing to be done for CCSShadow::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to be done for CCSShadow::DeInit()
            return true;
        }

        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            DataSize = sectionSize * 4;
            Data = new byte[DataSize];
            bStream.Read(Data, 0, DataSize);
            return true;
        }

    }
}
