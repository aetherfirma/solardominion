using UnityEngine;

namespace Logic.Utilities
{
    public struct AsteroidField
    {
        private readonly Vector3 _location;
        private readonly float _size;

        public Vector3 Location
        {
            get { return _location; }
        }

        public float Size
        {
            get { return _size; }
        }

        public AsteroidField(Vector3 location, float size)
        {
            _location = location;
            _size = size;
        }
    }
}