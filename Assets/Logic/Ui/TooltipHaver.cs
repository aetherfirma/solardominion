using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Ui
{
    public class TooltipHaver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public MessageTooltip Tooltip;
        public string Message;
        private Tooltip _tooltip;

        private Canvas GetParentCanvas()
        {
            var c = transform.GetComponentsInParent<Canvas>();
            return c[c.Length - 1];
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            DestroyTooltip();
            _tooltip = Tooltip.Create(GetParentCanvas().transform, Message);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DestroyTooltip();
        }

        private void OnDestroy()
        {
            DestroyTooltip();
        }

        private void DestroyTooltip()
        {
            if (_tooltip != null)
            {
                Destroy(_tooltip.gameObject);
                _tooltip = null;
            }
        }
    }
}