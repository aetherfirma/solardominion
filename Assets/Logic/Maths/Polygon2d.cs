using UnityEngine;

namespace Logic.Maths
{
    public class Polygon2D : PointInside
    {
        public Vector2[] Points;

        public Polygon2D(Vector2[] points)
        {
            Points = points;
        }

        public bool IsInside(Vector2 point, float playArea)
        {
            throw new System.NotImplementedException();
        }
    }
}