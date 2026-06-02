using OpenTK.Mathematics;
namespace StudioCCS.FileFormat.SceneObjects
{
    public class CCSCamera : CCSBaseObject
    {
        //Not defined in Setup section, but set by Animation.
        public int Unk1 = 0;
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero;
        public float FOV = 45.0f;
        public float UnkFloat = 0.0f;

        public CCSCamera(int _objectID, CCSFile _parentFile)
        {
            ObjectID = _objectID;
            ParentFile = _parentFile;
            ObjectType = CCSFile.SECTION_CAMERA;
        }

        public override bool Init()
        {
            //Currently nothing to be done for CCSCamera::Init()
            return true;
        }

        public override bool DeInit()
        {
            //Currently nothing to be done for CCSCamera::DeInit()
            return true;
        }

        public override bool Read(BinaryReader bStream, int sectionSize)
        {
            //No Parameters to read.
            return true;
        }
    }
}
