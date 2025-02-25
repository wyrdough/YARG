using UnityEngine;
using YARG.Core.Chart;

namespace YARG.Gameplay.Visuals
{
    public class StarpowerBar : MonoBehaviour
    {
        private static readonly int _fill = Shader.PropertyToID("Fill");
        private static readonly int _pulse = Shader.PropertyToID("Pulse");

        [SerializeField]
        private MeshRenderer _starpowerBar;

        private double _starpowerAmount;
        private bool _starpowerActive;

        public void SetStarpower(double starpowerAmount, bool starpowerActive, bool breMode = false)
        {
            if (!breMode)
            {
                // We aren't supposed to show SP after coda has started, per artist team
                _starpowerAmount = starpowerAmount;
                _starpowerActive = starpowerActive;
            }
            else
            {
                _starpowerAmount = 0;
                _starpowerActive = false;
            }

            _starpowerBar.material.SetFloat(_fill, (float) starpowerAmount);
        }

        public void PulseBar(Beatline beat)
        {
            if (beat.Type == BeatlineType.Weak)
                return;

            if (_starpowerAmount >= 0.5 || _starpowerActive)
            {
                _starpowerBar.material.SetFloat(_pulse, 1f);
            }
        }

        private void Update()
        {
            var mat = _starpowerBar.material;

            // Fade out the pulse
            float currentPulse = mat.GetFloat(_pulse);
            mat.SetFloat(_pulse, Mathf.Clamp01(currentPulse - Time.deltaTime * 6f));
        }
    }
}