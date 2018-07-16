using UnityEngine;

namespace Logic.Utilities
{
    public class AutoDestructingParticleSystem : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Start()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (!_particleSystem.IsAlive()) Destroy(gameObject);
        }
    }
}