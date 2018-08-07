using Logic.Gameplay.Rules;
using UnityEngine;

namespace Logic.Display
{
    public class RangeMarkers : MonoBehaviour
    {
        private Referee _referee;
        private Material _material;

        public void Setup(float shortRange, float mediumRange, float longRange, Vector3 center, Referee referee)
        {
            _material = GetComponent<MeshRenderer>().material;
            _referee = referee;
            _material.SetFloat("_ShortRange", shortRange);
            _material.SetFloat("_MediumRange", mediumRange);
            _material.SetFloat("_LongRange", longRange);
            _material.SetVector("_Center", center);
        }

        private void Update()
        {
            _material.SetVector("_MousePosition", _referee.MouseLocation - Vector3.down * 3);
        }
    }
}