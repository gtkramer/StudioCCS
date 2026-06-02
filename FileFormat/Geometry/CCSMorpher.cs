namespace StudioCCS.FileFormat.Geometry;

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

    public override CCSTreeNode ToNode()
    {
        var retNode = base.ToNode();
        retNode.Text += string.Format(" Base: {0}", ParentFile.GetSubObjectName(BaseModelID));
        return retNode;
    }
}
