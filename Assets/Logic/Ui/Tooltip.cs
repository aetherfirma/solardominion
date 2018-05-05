using UnityEngine;
using UnityEngine.UI;

namespace Logic.Ui
{
    public class Tooltip : MonoBehaviour
    {
        private Text _text;
        private RectTransform _rectTransform;
        private string _message;

        private void Start()
        {
            _text = transform.Find("Text").gameObject.GetComponent<Text>();
            _text.text = _message;
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            _rectTransform.sizeDelta = new Vector2(200, _text.preferredHeight + 20);
            _rectTransform.anchoredPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
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