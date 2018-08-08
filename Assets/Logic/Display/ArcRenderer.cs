using System;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic.Display
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class ArcRenderer : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        public float Radius, Width;
        [Range(0, 360)] public float StartDegrees, EndDegrees;
        public int Segments;
        public Gradient Gradient;

        private float _startRadian
        {
            get { return StartDegrees / 360 * (Mathf.PI * 2); }
            set { StartDegrees = value / (Mathf.PI * 2) * 360; }
        }

        private float _endRadian
        {
            get { return EndDegrees / 360 * (Mathf.PI * 2) + (StartDegrees > EndDegrees ? Mathf.PI * 2 : 0); }
            set { EndDegrees = value / (Mathf.PI * 2) * 360; }
        }

        private void Awake()
        {
            _lineRenderer = gameObject.GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            SetPoints();
        }

#if UNITY_EDITOR
        private void Update()
        {
            SetPoints();
        }
#endif

        private void SetPoints()
        {
            _lineRenderer.widthMultiplier = Width;
            _lineRenderer.positionCount = Segments + 1;
            if (Math.Abs(_startRadian - _endRadian % (Mathf.PI * 2)) < 0.01f) _lineRenderer.loop = true;
            if (Gradient != null) _lineRenderer.colorGradient = Gradient;
            var point = new Vector2().FromAngleAndMagnitude(_startRadian, Radius);
            _lineRenderer.SetPosition(0, new Vector3(point.x, point.y));
            var delta = (_endRadian - _startRadian) / Segments;
            for (var i = 1; i <= Segments; i++)
            {
                point = point.FromAngleAndMagnitude(_startRadian + delta * i, Radius);
                _lineRenderer.SetPosition(i, new Vector3(point.x, point.y));
            }
        }

        public static GameObject NewArc(Transform parent, float radius, float width, float start, float end,
            int segments, Color color)
        {
            var obj = new GameObject("Selection Ring");
            obj.transform.parent = parent;
            obj.transform.position = parent.transform.position;
            obj.transform.Rotate(-90, 0, 0);
            var arc = obj.AddComponent<ArcRenderer>();
            arc.Radius = radius;
            arc.Width = width;
            arc._startRadian = start;
            arc._endRadian = end;
            arc.Segments = segments;
            var gradient = new Gradient().GradientFromColor(color);
            arc.Gradient = gradient;

            arc.SetPoints();

            return obj;
        }

        public static GameObject CreatePentagon()
        {
            var outline = new GameObject("BaseOutline", typeof(LineRenderer));
            var outlineRenderer = outline.GetComponent<LineRenderer>();
            outlineRenderer.useWorldSpace = false;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            outlineRenderer.widthMultiplier = 0.25f * outline.transform.lossyScale.x;
            outlineRenderer.positionCount = 5;
            outlineRenderer.loop = true;
            for (int i = 0; i < 5; i++)
            {
                outlineRenderer.SetPosition(i,
                    new Vector2().FromAngleAndMagnitude(Mathf.PI * 2 / 5 * i + (Mathf.PI / 2), 2.5f).Vec2ToVec3());
            }

            return outline;
        }
    }
}