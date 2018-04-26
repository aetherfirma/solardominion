using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logic.Utilities
{
    public static class Extensions
    {
        public static Vector2 FromAngleAndMagnitude(this Vector2 vec, float angle, float magnitude)
        {
            vec.x = Mathf.Cos(angle) * magnitude;
            vec.y = Mathf.Sin(angle) * magnitude;
            return vec;
        }

        public static Vector3 Vec2ToVec3(this Vector2 vec2)
        {
            return new Vector3(vec2.x, 0, vec2.y);
        }

        public static Gradient GradientFromColor(this Gradient gradient, Color color)
        {
            gradient.SetKeys(new[] {new GradientColorKey(color, 0), new GradientColorKey(color, 1)},
                new[] {new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f)});
            return gradient;
        }

        public static GameObject[] FindChildrenWithTag(this GameObject gameObject, string tag)
        {
            return (from Transform transform in gameObject.transform where transform.gameObject.CompareTag(tag) select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithName(this GameObject gameObject, string name)
        {
            return (from Transform transform in gameObject.transform where transform.gameObject.name == name select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithStringInName(this GameObject gameObject, string fragment)
        {
            return (from Transform transform in gameObject.transform where transform.gameObject.name.Contains(fragment) select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithoutStringInName(this GameObject gameObject, string fragment)
        {
            return (from Transform transform in gameObject.transform where !transform.gameObject.name.Contains(fragment) select transform.gameObject).ToArray();
        }
    }

    public class DictionaryWithDefault<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public delegate TValue DefaultValueProvider();

        private DefaultValueProvider _default;

        public DictionaryWithDefault(TValue defaultValue) : base()
        {
            _default = () => defaultValue;
        }

        public DictionaryWithDefault(DefaultValueProvider defaultValueProvider) : base()
        {
            _default = defaultValueProvider;
        }

        public new TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value)) return value;
                value = _default();
                this[key] = value;
                return value;
            }
            set { base[key] = value; }
        }
    }
}