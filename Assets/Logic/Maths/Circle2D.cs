using UnityEngine;

namespace Logic.Maths
{
    public class Circle2D : PointInside
    {
        public Vector2 Centre;
        public float Radius;

        public Circle2D(Vector2 centre, float radius)
        {
            Centre = centre;
            Radius = radius;
        }

        public bool IsInside(Vector2 point, float playArea)
        {
            return Mathf.Abs(point.x) < playArea / 2 && Mathf.Abs(point.y) < playArea / 2 && (point - Centre).magnitude < Radius;
        }
    }
}