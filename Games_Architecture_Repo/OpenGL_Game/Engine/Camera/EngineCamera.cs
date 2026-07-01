using OpenTK.Mathematics;

namespace OpenGL_Game.Engine.Camera
{
    class EngineCamera
    {
        public Matrix4 View { get; private set; }
        public Matrix4 Projection { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Up { get; set; }

        public EngineCamera(Vector3 cameraPos, Vector3 targetPos, float ratio, float near, float far, float fovDegrees = 45f)
        {
            Up = Vector3.UnitY;
            Position = cameraPos;
            Direction = Vector3.Normalize(targetPos - cameraPos);
            UpdateView();
            SetPerspective(fovDegrees, ratio, near, far);
        }

        public void MoveForward(float move)
        {
            Position += move * Direction;
            UpdateView();
        }

        public void Translate(Vector3 move)
        {
            Position += move;
            UpdateView();
        }

        public void RotateY(float angle)
        {
            Direction = Matrix3.CreateRotationY(angle) * Direction;
            UpdateView();
        }

        public void SetPerspective(float fovDegrees, float ratio, float near, float far)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fovDegrees), ratio, near, far);
        }

        public void UpdateView()
        {
            View = Matrix4.LookAt(Position, Position + Direction, Up);
        }
    }
}