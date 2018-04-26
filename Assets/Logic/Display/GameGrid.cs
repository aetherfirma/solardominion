using Logic.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic.Display
{
    public class GameGrid : MonoBehaviour
    {
        [Range(0,120)]
        public int Size;
        [Range(1,10)]
        public int Gradiation;

        private GameObject[] _lines;

        private void Start()
        {
            float min, max;
            max = Size / 2f;
            min = -max;

            var current = min;
            while (current <= max)
            {
                var xLineObject = new GameObject("GridLine");
                xLineObject.transform.parent = transform;
                var xLine = xLineObject.AddComponent<LineRenderer>();
                xLine.useWorldSpace = true;
                xLine.shadowCastingMode = ShadowCastingMode.Off;
                xLine.receiveShadows = false;
                xLine.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
                xLine.colorGradient = new Gradient().GradientFromColor(new Color(1f, 1f, 1f, 0.1f));
                xLine.widthMultiplier = 0.1f;
                xLine.positionCount = 2;
                xLine.SetPosition(0, new Vector3(current, 0, min));
                xLine.SetPosition(1, new Vector3(current, 0, max));
                
                var yLineObject = new GameObject("GridLine");
                yLineObject.transform.parent = transform;
                var yLine = yLineObject.AddComponent<LineRenderer>();
                yLine.useWorldSpace = true;
                yLine.shadowCastingMode = ShadowCastingMode.Off;
                yLine.receiveShadows = false;
                yLine.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
                yLine.colorGradient = new Gradient().GradientFromColor(new Color(1f, 1f, 1f, 0.1f));
                yLine.widthMultiplier = 0.1f;
                yLine.positionCount = 2;
                yLine.SetPosition(0, new Vector3(min, 0, current));
                yLine.SetPosition(1, new Vector3(max, 0, current));

                current += Gradiation;
            }
        }
    }
}