using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace StudioCCS.FileFormat.Raw
{
    public class CCSFBPage : CCSBaseObject
    {
        public byte[] Data;
        public int DataSize;

        public CCSFBPage(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_FBPAGE;
        }

        public override bool Init()
        {
            //Currently nothing to do for CCSFBPage::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to do for CCSFBPage::DeInit()
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
