using UnityEngine;

namespace Logic.Display
{
    public class SelectionEffect : MonoBehaviour
    {
        private float _scale;
        private float _yScale;
        private float _offset;
        
        private void Start()
        {
            _scale = transform.localScale.x;
            _yScale = transform.localScale.y;
            _offset = Random.value * Mathf.PI * 2;
        }

        private void Update()
        {
            var scale = Mathf.Sin(Time.time + _offset) * 0.1f + 1f;
            transform.localScale = new Vector3(scale * _scale, _yScale, scale * _scale);
        }
    }
}