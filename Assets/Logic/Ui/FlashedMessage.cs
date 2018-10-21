using UnityEngine;
using UnityEngine.UI;

namespace Logic.Ui
{
    public class FlashedMessage
    {
        public readonly float DiesAt;
        public readonly RectTransform Transform;

        public FlashedMessage(string message, float ttl, RectTransform uiElement, Transform parent)
        {
            DiesAt = Time.time + ttl;
            Transform = Object.Instantiate(uiElement, parent);
            var component = Transform.Find("Message").GetComponent<Text>();
            component.text = message;
            Transform.sizeDelta = new Vector2(Transform.sizeDelta.x, component.preferredHeight + 25);
//            if (icon != null) 
        }
    }
}