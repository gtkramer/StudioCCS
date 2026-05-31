using System.Drawing;
using System.IO;

namespace StudioCCS.FileFormat.Materials
{
    public class CCSClut : CCSBaseObject
    {
        public int ColorCount;
        public int BlitGroup;
        public Color[] Palette = null;
        public bool HasAlpha = false;

        public CCSClut(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_CLUT;
        }

        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            BlitGroup = bStream.ReadInt32();

            bStream.ReadInt32();
            bStream.ReadInt32();
            ColorCount = bStream.ReadInt32();

            Palette = new Color[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                Palette[i] = Util.ReadColorRGBA32(bStream);
                HasAlpha |= Palette[i].A != 0xff;
            }

            return true;
        }

        public override bool Init()
        {
            //Currently nothing to do for CCSClut::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to do for CCSClut::DeInit()
            return true;
        }

        public override CCSTreeNode ToNode()
        {
            var retNode = base.ToNode();
            retNode.Text += string.Format(" ({0} Colors)", ColorCount);
            return retNode;
        }
    }
}
