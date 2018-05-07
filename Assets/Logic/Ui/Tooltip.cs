using UnityEngine;

namespace Logic.Ui
{
    public class Tooltip : MonoBehaviour
    {
        protected RectTransform RectTransform;
        public Vector2 Offset;
        
        public void Start()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public void Update()
        {
            var inputX = Input.mousePosition.x + Offset.x;
            var outputX = inputX + RectTransform.sizeDelta.x + 20 < Screen.width
                ? inputX
                : inputX - RectTransform.sizeDelta.x;
            var inputY = Input.mousePosition.y + Offset.y;
            var outputY = inputY + RectTransform.sizeDelta.y + 20 < Screen.height
                ? inputY
                : inputY - RectTransform.sizeDelta.y;
            RectTransform.anchoredPosition = new Vector2(outputX, outputY);
        }
    }
}