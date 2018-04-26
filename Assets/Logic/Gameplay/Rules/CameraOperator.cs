using UnityEngine;

namespace Logic.Gameplay.Rules
{
    public class CameraOperator
    {
        private readonly Transform _cameraTransform;
        public Vector3 Location = new Vector3(0, 5, 0);
        public float Direction = 0, Zoom = 1;

        public float DirectionRadians
        {
            get { return Direction / 180f * Mathf.PI; }
        }

        public CameraOperator(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        public void EnforceCameraBoundaries(float playArea)
        {
            Location = Vector3.ClampMagnitude(Location, playArea / 2f);
        }

        public void UpdateCamera()
        {
            _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, Location, Time.deltaTime);
            var scale = Mathf.Lerp(_cameraTransform.localScale.x, Zoom, Time.deltaTime);
            _cameraTransform.localScale = new Vector3(scale, scale, scale);
            var newRotation = Quaternion.Euler(0, Direction, 0);
            _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, newRotation, Time.deltaTime);
        }

        public void SetCameraPosition(Vector3 location, float direction, float zoom)
        {
            Debug.Log(location + " " + direction + " " + zoom);
            Direction = direction;
            Location = location;
            Zoom = zoom;
        }

    }
}