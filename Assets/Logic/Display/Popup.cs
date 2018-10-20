using TMPro;
using UnityEngine;

namespace Logic.Display
{
    public class Popup : MonoBehaviour
    {
        public bool Rising;
        public float RisingRate;
        public float Lifespan = float.PositiveInfinity;
        private float _lifetime;
        public RectTransform Canvas;
        public TextMeshProUGUI UIText;

        public string Text
        {
            get { return UIText.text; }
            set { UIText.text = value; }
        }

        private void Update()
        {
            Canvas.sizeDelta = new Vector2(UIText.preferredWidth + 20, UIText.preferredHeight + 90);
            if (Rising)
            {
                _lifetime += Time.deltaTime;
                if (_lifetime > Lifespan) Destroy();
                transform.position += Vector3.up * Time.deltaTime * RisingRate;
            }
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public Popup Clone(string message, Vector3 position, float rate, float lifespan)
        {
            var clone = Clone(message, position);
            clone.Rising = true;
            clone.RisingRate = rate;
            clone.Lifespan = lifespan;
            return clone;
        }

        public Popup Clone(string message, Vector3 position)
        {
            var clone = Instantiate(this);
            clone.transform.position = position;
            clone.Text = message;
            return clone;
        }
    }
}