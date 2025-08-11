using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu
{
    public class DiscreteProgressDisplay : MonoBehaviour
    {
        // TODO: _stepSprites should really be a prefab we instantiate however many we need so
        // it doesn't have to be all set up in the inspector

        // Step one is separate from the rest because it is just a single dot, other steps require a "tail"
        [SerializeField]
        private Image _stepOneSprite;
        [SerializeField]
        private GameObject _stepContainer;
        [SerializeField]
        private Image _stepPrefab;

        private int _stepCount = 1;
        private int _currentStep;

        private Image[] _stepSprites;

        // TODO: Tune these colors to be in line with the usual branding colors
        private Color _completedColor   = Color.green;
        private Color _uncompletedColor = Color.grey;

        public void Initialize(int steps = 4)
        {
            // Calculate scale necessary to fit all steps in the container
            var stepsWidth = _stepPrefab.rectTransform.rect.width * steps;
            var containerWidth = _stepContainer.GetComponent<RectTransform>().rect.width;
            var scale = containerWidth / stepsWidth;

            _stepCount = steps;
            _stepSprites = new Image[steps - 1];
            _currentStep = 0;
            _stepOneSprite.color = _completedColor;
            // - 1 because the first step isn't something we are instantiating and is always complete
            for (int i = 0; i < steps - 1; i++)
            {
                var step = Instantiate(_stepPrefab, _stepContainer.transform);
                step.rectTransform.localScale = new Vector3(scale, scale, 1);
                _stepSprites[i] = step;
            }

            foreach (var step in _stepSprites)
            {
                step.color = _uncompletedColor;
            }
        }

        public void ProgressToNextStep()
        {
            _currentStep++;
            if (_currentStep >= _stepCount)
            {
                return;
            }

            _stepSprites[_currentStep - 1].color = _completedColor;
        }

        public void ReturnToPreviousStep()
        {
            _currentStep--;
            if (_currentStep <= 1)
            {
                return;
            }

            _stepSprites[_currentStep].color = _completedColor;
        }

        public void SetStepCount(int stepCount)
        {
            if (stepCount < 1 || stepCount > _stepSprites.Length)
            {
                Debug.LogError($"Step count must be between 1 and {_stepSprites.Length - 1}");
                return;
            }

            _stepCount = stepCount;
            for (int i = 0; i < _stepCount; i++)
            {
                if (i < stepCount - 2)
                {
                    _stepSprites[i].color = _completedColor;
                }
                else
                {
                    _stepSprites[i].color = _uncompletedColor;
                }
            }
        }

        public void ResetProgress()
        {
            _currentStep = 1;
            _stepOneSprite.color = _completedColor;
            foreach (var step in _stepSprites)
            {
                step.color = _uncompletedColor;
            }
        }
    }
}