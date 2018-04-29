using UnityEngine;

namespace Logic.Maths
{
    public interface PointInside
    {
        bool IsInside(Vector2 point, float playArea);
    }
}