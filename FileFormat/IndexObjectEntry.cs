using System.IO;

namespace StudioCCS.FileFormat
{
    public class IndexObjectEntry
    {
        public string ObjectName = "";
        public short FileID = 0;
        public CCSBaseObject ObjectRef = null;
        public int ObjectType = 0;
        public int ObjectOffset = 0;

        public void Read(BinaryReader bStream)
        {
            ObjectName = Util.ReadString(bStream, 0x1e);
            FileID = bStream.ReadInt16();
        }
    }
}
