using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace StudioCCS.libCCS
{
    /// <summary>
    /// Description of CCSExt.
    /// </summary>
    public class CCSExt : CCSBaseObject
    {
        public int ReferencedParentID;
        public int ReferencedObjectID;

        public CCSExt(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_EXTERNAL;
        }

        public override bool Init()
        {
            //Currently nothing to be done for CCSExt::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to be done for CCSExt::DeInit()
            return true;
        }

        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            ReferencedParentID = bStream.ReadInt32();
            ReferencedObjectID = bStream.ReadInt32();

            return true;
        }
    }
}
