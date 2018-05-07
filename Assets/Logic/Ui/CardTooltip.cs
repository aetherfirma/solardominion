using Logic.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Logic.Ui
{
    public class CardTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Texture2D Image;
        public RectTransform ParentTransform;
        private GameObject _tooltip;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tooltip = new GameObject("card tooltip", typeof(RectTransform), typeof(Image), typeof(Tooltip));
            _tooltip.SetAsChild(ParentTransform);
            var image = _tooltip.GetComponent<Image>();
            image.sprite = Sprite.Create(Image, new Rect(0, 0, Image.width, Image.height),
                new Vector2(Image.width / 2f, Image.height / 2f));
            image.raycastTarget = false;
            var rectTransform = image.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(Image.width / 3f, Image.height / 3f);
            _tooltip.GetComponent<Tooltip>().Offset = new Vector2(Image.width / 6f, Image.height / 6f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null) Destroy(_tooltip);
        }
    }
}