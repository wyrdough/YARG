using UnityEngine;

namespace YARG.Helpers.Authoring
{
    // WARNING: Changing this could break themes or venues!
    //
    // This script is used a lot in theme creation.
    // Changing the serialized fields in this file will result in older themes
    // not working properly. Only change if you need to.

    [RequireComponent(typeof(ParticleSystem))]
    public class EffectParticle : MonoBehaviour
    {
        private static readonly int _emissionColor = Shader.PropertyToID("_EmissionColor");

        [Space]
        [SerializeField]
        private bool _allowColoring = true;
        [SerializeField]
        private bool _keepAlphaWhenColoring = true;

        [Space]
        [SerializeField]
        private bool _setEmissionWhenColoring;
        [SerializeField]
        private float _emissionColorMultiplier = 1f;

        private ParticleSystem         _particleSystem;
        private ParticleSystemRenderer _particleSystemRenderer;
        private bool                   _breMode;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
        }

        public void SetColor(Color color)
        {
            if (!_allowColoring) return;

            // Get the main particle module
            var m = _particleSystem.main;

            // Get the preferred color
            var c = color;
            if (_keepAlphaWhenColoring)
            {
                c.a = m.startColor.color.a;
            }

            // Set the color
            m.startColor = c;

            // Now try to set the emission color
            if (!_setEmissionWhenColoring || _particleSystemRenderer == null) return;

            // Set the emission color
            var material = _particleSystemRenderer.material;
            material.color = color;
            material.SetColor(_emissionColor, color * _emissionColorMultiplier);
        }

        public void Play(bool breMode = false)
        {
            // Prevent double starts
            if (_particleSystem.main.loop && _particleSystem.isEmitting) return;

            // Double the duration of all effects in BRE mode
            if (breMode && !_breMode && (_particleSystem.name == "Sparkles" || _particleSystem.name == "Shards"))
            {
                _breMode = true;
                var particleMain = _particleSystem.main;
                var particleEmitter = _particleSystem.emission;

                if (particleEmitter.burstCount > 0)
                {
                    particleMain.startLifetimeMultiplier = 10;
                    particleMain.maxParticles = 10000;
                    var burst = particleEmitter.GetBurst(0);
                    burst.minCount *= 20;
                    burst.maxCount *= 20;
                    particleEmitter.SetBurst(0, burst);
                }
                else
                {
                    particleMain.startLifetimeMultiplier = 5;
                }
            }

            _particleSystem.Play();
        }

        public void Stop()
        {
            // Prevent double stops
            if (_particleSystem.main.loop && !_particleSystem.isEmitting) return;

            _particleSystem.Stop();
        }
    }
}