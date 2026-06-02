namespace StudioCCS.FileFormat
{
    public class IndexFileEntry
    {
        public string FileName = "";
        public List<int> ObjectIDs = new List<int>();

        public void Read(BinaryReader bStream)
        {
            FileName = Util.ReadString(bStream);
        }

        public void AddObjectID(int _objectID)
        {
            ObjectIDs.Add(_objectID);
        }

    }
}
