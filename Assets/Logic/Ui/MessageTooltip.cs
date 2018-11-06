using TMPro;
using UnityEngine;

namespace Logic.Ui
{
    public class MessageTooltip : Tooltip
    {
        private TextMeshProUGUI _text;
        private string _message;

        public new void Start()
        {
            _text = transform.Find("Text").gameObject.GetComponent<TextMeshProUGUI>();
            _text.text = _message;
            base.Start();
        }

        public new void Update()
        {
            RectTransform.sizeDelta = new Vector2(200, _text.preferredHeight + 20);
            base.Update();
        }

        public Tooltip Create(Transform parent, string message)
        {
            var tip = Instantiate(this, parent);
            tip._message = message;
            tip.GetComponent<RectTransform>().anchoredPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            return tip;
        }
    }
}