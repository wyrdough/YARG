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

        private Quaternion           _initialRotation;
        private float                _initialSpeedMultiplier;
        private float                _initialLifetimeMultiplier;
        private float                _initialStartSpeedMultiplier;
        private int                  _initialMaxParticles;
        private ParticleSystem.Burst _initialBurst;
        private ParticleSystemShapeType _initialShapeType;


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
            if (breMode != _breMode && (_particleSystem.name == "Sparkles" || _particleSystem.name == "Shards"))
            {
                SetBREMode(breMode);
            }

            _particleSystem.Play();
        }

        private void SetBREMode(bool active)
        {
            if (active == _breMode) return;

            var particleMain = _particleSystem.main;
            var particleEmitter = _particleSystem.emission;
            var particleShape = _particleSystem.shape;
            var particleRotation = _particleSystem.transform.rotation;

            if (active)
            {
                _breMode = true;

                _initialRotation = _particleSystem.transform.rotation;
                _initialSpeedMultiplier = particleMain.startSpeedMultiplier;
                _initialLifetimeMultiplier = particleMain.startLifetimeMultiplier;
                _initialStartSpeedMultiplier = particleMain.startSpeedMultiplier;
                _initialMaxParticles = particleMain.maxParticles;
                _initialShapeType = particleShape.shapeType;
                if (particleEmitter.burstCount > 0)
                {
                    _initialBurst = particleEmitter.GetBurst(0);
                }

                if (particleEmitter.burstCount > 0)
                {
                    particleRotation.x = 180;
                    _particleSystem.transform.rotation = particleRotation;
                    // particleShape.angle = 60;
                    var speed = particleMain.startSpeed;
                    // speed.curveMultiplier = 2;
                    // particleMain.startSpeed = speed;
                    particleMain.startSpeedMultiplier = 2f;
                    // particleMain.gravityModifierMultiplier = 2f;
                    particleMain.startLifetimeMultiplier = 1.2f;
                    particleMain.maxParticles = 10000;
                    if (particleEmitter.burstCount > 0)
                    {
                        var burst = particleEmitter.GetBurst(0);
                        burst.minCount *= 5;
                        burst.maxCount *= 5;
                        particleEmitter.SetBurst(0, burst);
                    }
                }
                else
                {
                    particleShape.shapeType = ParticleSystemShapeType.Cone;
                    particleShape.randomDirectionAmount = 30f;
                    particleMain.startLifetimeMultiplier = 2;
                }
            }
            else
            {
                _breMode = false;

                particleRotation = _initialRotation;
                _particleSystem.transform.rotation = particleRotation;
                particleMain.startSpeedMultiplier = _initialSpeedMultiplier;
                particleMain.startLifetimeMultiplier = _initialLifetimeMultiplier;
                particleMain.startSpeedMultiplier = _initialStartSpeedMultiplier;
                particleMain.maxParticles = _initialMaxParticles;
                particleShape.shapeType = _initialShapeType;
                if (particleEmitter.burstCount > 0)
                {
                    particleEmitter.SetBurst(0, _initialBurst);
                }
            }
        }

        public void Stop()
        {
            // Prevent double stops
            if (_particleSystem.main.loop && !_particleSystem.isEmitting) return;

            _particleSystem.Stop();
        }
    }
}