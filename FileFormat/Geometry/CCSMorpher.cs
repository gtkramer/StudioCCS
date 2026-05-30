using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace StudioCCS.FileFormat.Geometry
{
    /// <summary>
    /// Description of CCSMorpher.
    /// </summary>
    public class CCSMorpher : CCSBaseObject
    {
        public int BaseModelID = 0;

        //Helper Refs
        private CCSModel BaseModelRef = null;

        public CCSMorpher(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_MORPHER;
        }

        public override bool Init()
        {
            BaseModelRef = ParentFile.GetObject<CCSModel>(BaseModelID);
            return true;
        }
        public override bool DeInit()
        {
            return true;
        }
        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            BaseModelID = bStream.ReadInt32();
            return true;
        }

        public override CcsTreeNode ToNode()
        {
            var retNode = base.ToNode();
            retNode.Text += string.Format(" Base: {0}", ParentFile.GetSubObjectName(BaseModelID));
            return retNode;
        }
    }
}
