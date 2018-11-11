using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Logic.Maths;
using UnityEngine;
using UnityEngine.Networking;
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
            return (from Transform transform in gameObject.transform
                where transform.gameObject.CompareTag(tag)
                select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithName(this GameObject gameObject, string name)
        {
            return (from Transform transform in gameObject.transform
                where transform.gameObject.name == name
                select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithStringInName(this GameObject gameObject, string fragment)
        {
            return (from Transform transform in gameObject.transform
                where transform.gameObject.name.Contains(fragment)
                select transform.gameObject).ToArray();
        }

        public static GameObject[] FindChildrenWithoutStringInName(this GameObject gameObject, string fragment)
        {
            return (from Transform transform in gameObject.transform
                where !transform.gameObject.name.Contains(fragment)
                select transform.gameObject).ToArray();
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

        public static int[] RerollFailures(this int[] rolls, int training, WellRng rng)
        {
            var successes = rolls.Where(r => r >= training).ToList();
            successes.AddRange(rng.D6(rolls.Length - successes.Count));
            return successes.ToArray();
        }

        public static int[] RerollSuccesses(this int[] rolls, int training, WellRng rng)
        {
            var failures = rolls.Where(r => r < training).ToList();
            failures.AddRange(rng.D6(rolls.Length - failures.Count));
            return failures.ToArray();
        }

        public static string DescribeDiceRolls(this int[] rolls)
        {
            var simple = string.Join(", ", rolls.Select(v => v.ToString()).ToArray());
            
            var results = new DictionaryWithDefault<int, int>(0);
            foreach (var roll in rolls)
            {
                results[roll] += 1;
            }

            var complex = string.Join(", ", results.Select(pair => string.Format("{1} {0}s", pair.Key, pair.Value)).ToArray());

            return complex.Length >= simple.Length ? simple : complex;
        }

        public static void BasicAuth(this UnityWebRequest request, string username, string password)
        {
            request.SetRequestHeader("Authorization",
                string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", username, password)))));
        }

        public static float Distance(this Vector3 a, Vector3 b)
        {
            return (a - b).magnitude;
        }

        public static float DistanceToLine(this Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var upper = (p2.y - p1.y) * p0.x - (p2.x - p1.x) * p0.y + p2.x * p1.y - p2.y * p1.x;
            var lower = Mathf.Pow(p2.y - p1.y, 2) + Mathf.Pow(p2.x - p1.x, 2);
            return Mathf.Abs(upper) / Mathf.Sqrt(lower);
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