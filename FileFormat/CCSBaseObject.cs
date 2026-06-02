namespace StudioCCS.FileFormat;

public abstract class CCSBaseObject
{
    public int ObjectID = 0;
    public int ObjectType = 0;
    public CCSFile ParentFile;

    public virtual CCSTreeNode ToNode()
    {
        CCSTreeNode retNode = new CCSTreeNode(string.Format("{0}: {1}", ObjectID, ParentFile.GetSubObjectName(ObjectID)))
        {
            Tag = new TreeNodeTag(ParentFile, ObjectID, ObjectType)
        };
        return retNode;
    }

    public abstract bool Init();
    public abstract bool DeInit();
    public abstract bool Read(BinaryReader bStream, int sectionSize);
}
