using System;
using System.IO;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core.Audio;
using YARG.Core.Input;
using YARG.Menu.Navigation;
using YARG.Player;
using YARG.Settings;

namespace YARG.Menu.Calibrator
{
    public class VideoCalibrator : MonoBehaviour
    {
        [SerializeField]
        private GameObject _startingStateContainer;
        [SerializeField]
        private GameObject _videoCalibratorContainer;
        [SerializeField]
        private Slider _slider;
        [SerializeField]
        private Image _sliderFill;
        [Space]
        [SerializeField]
        private TextMeshProUGUI _adjustmentText;

        private CalibrationMenu _calibrationMenu;

        private CalibrationState _state;

        private Tweener  _calibrationTween;
        private Sequence _calibrationSequence;
        private Sequence _yellowSequence;
        private Sequence _greenSequence;

        private Color _startColor;

        private const float TIME_BETWEEN_CLICKS = 0.75f;

        private double _totalAdjustment = 0;

        #nullable enable
        private StemMixer? _mixer;
        #nullable disable

        private void OnEnable()
        {
            _calibrationMenu = GetComponentInParent<CalibrationMenu>();

            _state = CalibrationState.Starting;
            _startingStateContainer.SetActive(true);
            _videoCalibratorContainer.SetActive(false);
            _startColor = _sliderFill.color;

            _totalAdjustment = SettingsManager.Settings.VideoCalibration.Value / 1000.0;

            SetAdjustmentText();

            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Green, "Menu.Common.Confirm", StartCalibration),
            }, false));
        }

        public void StartCalibration()
        {
            _state = CalibrationState.Calibrating;
            _startingStateContainer.SetActive(false);
            _videoCalibratorContainer.SetActive(true);

            Navigator.Instance.PushScheme(new NavigationScheme(new ()
            {
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Back),
                new NavigationScheme.Entry(MenuAction.Left, "Adjust 10ms", () => AdjustAudio(-0.01)),
                new NavigationScheme.Entry(MenuAction.Right, "Adjust 10ms", () => AdjustAudio(0.01)),
            }, false));

            PlayAudio();

            _calibrationTween = _slider.DOValue(1.0f, TIME_BETWEEN_CLICKS * 4)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetAutoKill(false)
                .Pause();
        }

        private void PlayAudio()
        {
            const float SPEED = 1f;
            const double VOLUME = 1.0;
            var file = Path.Combine(Application.streamingAssetsPath, "calibration_music.ogg");
            if (_mixer == null)
            {
                _mixer = GlobalAudioHandler.LoadCustomFile(file, SPEED, VOLUME);
                _mixer.SongEnd += PlayAudio;
            }
            _mixer.SetPosition((TIME_BETWEEN_CLICKS * 4) + _totalAdjustment);
            _mixer.Play(true);
            _calibrationTween.Restart();
            SetColors();
        }

        private void AdjustAudio(double adjust)
        {
            var pos = _mixer.GetPosition();
            _mixer.SetPosition(pos + adjust);
            _totalAdjustment += adjust;
            SetAdjustmentText();
        }

        private void SetAdjustmentText()
        {
            string calibrationText = $"Current Calibration {Math.Round(_totalAdjustment * 1000)}ms";
            _adjustmentText.text = calibrationText;
        }

        private async void SetColors()
        {
            await UniTask.WaitForSeconds(TIME_BETWEEN_CLICKS);
            _sliderFill.color = Color.yellow;
            // Wait for next frame and switch color back
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            _sliderFill.color = _startColor;
            await UniTask.WaitForSeconds(TIME_BETWEEN_CLICKS);
            _sliderFill.color = Color.yellow;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            _sliderFill.color = _startColor;
            await UniTask.WaitForSeconds(TIME_BETWEEN_CLICKS);
            _sliderFill.color = Color.yellow;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            _sliderFill.color = _startColor;
            await UniTask.WaitForSeconds(TIME_BETWEEN_CLICKS);
            _sliderFill.color = Color.red;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            _sliderFill.color = _startColor;
        }

        private void Back()
        {
            if (_state == CalibrationState.Calibrating)
            {
                _calibrationSequence.Kill();
                _mixer?.Pause();
                Navigator.Instance.PopScheme();
                SettingsManager.Settings.VideoCalibration.Value = (int) Math.Round(_totalAdjustment * 1000);
            }

            Navigator.Instance.PopScheme();
            _calibrationMenu.Back();
        }

        private void OnDestroy()
        {
            _mixer?.Dispose();
        }

        private enum CalibrationState
        {
            Starting,
            Calibrating
        }
    }
}