using System;
using System.Collections.Generic;
using System.Linq;
using Logic.Maths;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public static TSource Random<TSource>(this IEnumerable<TSource> source, WellRng rng)
        {
            var array = source.ToArray();
            return array[rng.NextInt(0, array.Length - 1)];
        }

        public static void DestroyAllChildren(this Transform transform)
        {
            foreach (Transform child in transform)
            {
                Object.Destroy(child.gameObject);
            }
        }

        public static void DestroyAllChildren(this Transform transform, Func<GameObject, bool> selector)
        {
            foreach (Transform child in transform)
            {
                if (selector(child.gameObject)) Object.Destroy(child.gameObject);
            }
        }

        public static void SetAsChild(this GameObject gameObject, Transform parent)
        {
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(1, 1, 1);
        }

        public static int Successes(this int[] rolls, int training)
        {
            return rolls.Count(r => r >= training);
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