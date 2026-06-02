using OpenTK.Mathematics;
namespace StudioCCS.Rendering
{
    public class ArcBallCamera
    {
        public Vector3 Position = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Rotation = new Vector3(180.0f, -45.0f, .0f);
        public Vector3 Target = new Vector3(0.0f, 0.0f, 0.0f);
        public float Distance = 10.0f;

        public ArcBallCamera()
        {

        }

        private void Clamp()
        {
            // Stop just short of straight up/down. At exactly +/-90 deg the view
            // direction aligns with the LookAt up-vector (+Y), the camera basis
            // becomes degenerate, and the view/gizmo flip 180 deg (gimbal lock).
            // Clamping below the pole keeps the turntable smooth and level.
            if (Rotation.Y > 89.9f)
            {
                Rotation.Y = 89.9f;
            }

            if (Rotation.Y < -89.9f)
            {
                Rotation.Y = -89.9f;
            }
            //Waiting for this to bug up
            if (Rotation.X > 360.0f)
            {
                Rotation.X = 0.0f;
            }

            if (Rotation.X < 0)
            {
                Rotation.X = 360.0f;
            }

            if (Distance < 0.1f)
            {
                Distance = 0.1f;
            }
        }

        public Matrix4 GetMatrix()
        {
            Calculate();
            Matrix4 cameraMatrix = Matrix4.LookAt(Position, Vector3.Zero, Vector3.UnitY);

            return Matrix4.CreateTranslation(Target) * cameraMatrix;
        }

        public Matrix4 GetMatrixDistanced(float dist)
        {
            //Clamp();
            const float rads = (float)(Math.PI / 180.0f);
            var CamPos = new Vector3();
            CamPos.X = (float)(dist * -Math.Sin(Rotation.X * rads) * Math.Cos(Rotation.Y * rads));
            CamPos.Y = (float)(dist * -Math.Sin(Rotation.Y * rads));
            CamPos.Z = -(float)(-dist * Math.Cos(Rotation.X * rads) * Math.Cos(Rotation.Y * rads));

            Matrix4 cameraMatrix = Matrix4.LookAt(CamPos, Vector3.Zero, Vector3.UnitY);
            return Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f) * cameraMatrix;

        }

        private void Calculate()
        {
            Clamp();
            const float rads = (float)(Math.PI / 180.0f);
            Position.X = (float)(Distance * -Math.Sin(Rotation.X * rads) * Math.Cos(Rotation.Y * rads));
            Position.Y = (float)(Distance * -Math.Sin(Rotation.Y * rads));
            Position.Z = -(float)(-Distance * Math.Cos(Rotation.X * rads) * Math.Cos(Rotation.Y * rads));
        }
    }
}
