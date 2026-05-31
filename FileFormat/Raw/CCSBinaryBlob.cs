using System.IO;

namespace StudioCCS.FileFormat.Raw
{
    public class CCSBinaryBlob : CCSBaseObject
    {
        public byte[] Data;
        public int DataSize;

        public CCSBinaryBlob(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_BINARYBLOB;
        }

        public override bool Init()
        {
            //Currently nothing to be done for CCSBinaryBlob::Init()
            return true;
        }
        public override bool DeInit()
        {
            //Currently nothing to be done for CCSBinaryBlob::DeInit()
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
