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
    /// <summary>
    /// Description of CCSLayer.
    /// </summary>
    public class CCSLayer : CCSBaseObject
    {
        public byte[] Data;
        public int DataSize;

        public CCSLayer(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_LAYER;
        }

        public override bool Init()
        {
            //Currently nothing to be done for CCSLayer::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to be done for CCSLayer::DeInit()
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
