using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Input;
using YARG.Menu.Navigation;

namespace YARG.Menu.Calibrator
{
    public class CalibrationMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject _menuContainer;
        [SerializeField]
        private GameObject _audioCalibrator;
        [SerializeField]
        private GameObject _videoCalibrator;

        private CalibrationState _currentState = CalibrationState.Menu;

        private void OnEnable()
        {
            _currentState = CalibrationState.Menu;
            _audioCalibrator.SetActive(false);
            _videoCalibrator.SetActive(false);

            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                NavigationScheme.Entry.NavigateSelect,
                NavigationScheme.Entry.NavigateUp,
                NavigationScheme.Entry.NavigateDown,
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Back)
            }, false));
        }

        public void StartAudioCalibration()
        {
            // Turn off the menu and turn on the audio calibration
            _currentState = CalibrationState.Audio;
            _menuContainer.SetActive(false);
            _audioCalibrator.SetActive(true);
            _videoCalibrator.SetActive(false);
        }

        public void StartVideoCalibration()
        {
            _currentState = CalibrationState.Video;
            _menuContainer.SetActive(false);
            _audioCalibrator.SetActive(false);
            _videoCalibrator.SetActive(true);
        }

        private void ShowMenu()
        {
            _currentState = CalibrationState.Menu;
            _menuContainer.SetActive(true);
            _audioCalibrator.SetActive(false);
            _videoCalibrator.SetActive(false);
        }

        public void Back()
        {
            // Return to the main menu
            switch (_currentState)
            {
                case CalibrationState.Menu:
                    Navigator.Instance.PopScheme();
                    GlobalVariables.Instance.LoadScene(SceneIndex.Menu);
                    break;
                default:
                    ShowMenu();
                    break;
            }
        }

        private enum CalibrationState
        {
            Menu,
            Audio,
            Video
        }

    }
}