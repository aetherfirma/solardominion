using Logic.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic.Display
{
    [RequireComponent(typeof(LineRenderer))]
    public class PentagonRenderer : MonoBehaviour
    {
        private TextMeshPro _textPrefab;
        
        public float Width;
        private float _setWidth;
        public Color Color = Color.white;
        private Color _displayedColor;
        private LineRenderer _outlineRenderer;
        public string Text = "";
        private string _displayedText;
        private TextMeshPro _textMesh;

        private void Start()
        {
            _outlineRenderer = GetComponent<LineRenderer>();
            _outlineRenderer.useWorldSpace = false;
            _outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _outlineRenderer.receiveShadows = false;
            _outlineRenderer.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            _outlineRenderer.positionCount = 5;
            _outlineRenderer.loop = true;
            for (int i = 0; i < 5; i++)
            {
                _outlineRenderer.SetPosition(i,
                    new Vector2().FromAngleAndMagnitude(Mathf.PI * 2 / 5 * i + (Mathf.PI / 2), 2.5f).Vec2ToVec3());
            }
        }

        private void Update()
        {
            var width = Width * transform.lossyScale.x;
            if (_setWidth != width)
            {
                _outlineRenderer.widthMultiplier = width;
                _setWidth = width;
            }

            if (_displayedColor != Color)
            {
                _outlineRenderer.colorGradient = new Gradient().GradientFromColor(Color);
                _displayedColor = Color;
            }

            if (_displayedText != Text)
            {
                if (_textMesh != null && Text == "")
                {
                    Destroy(_textMesh.gameObject);
                    _textMesh = null;
                }
                else if (_textMesh == null && Text != "")
                {
                    var obj = Instantiate(_textPrefab.gameObject, transform);
                    _textMesh = obj.GetComponent<TextMeshPro>();
                    _textMesh.transform.localPosition = new Vector3(0,0,-5f);
                    _textMesh.transform.localRotation = Quaternion.Euler(90,0,0);
                }

                if (Text != "")
                {
                    _textMesh.text = Text;
                }
            }
        }

        public static GameObject CreatePentagon(float width, TextMeshPro textPrefab)
        {
            var pentagonObject = new GameObject("Pentagon", typeof(PentagonRenderer));
            var pentagon = pentagonObject.GetComponent<PentagonRenderer>();
            pentagon._textPrefab = textPrefab;
            pentagon.Width = width;
            return pentagonObject;
        }
    }
}