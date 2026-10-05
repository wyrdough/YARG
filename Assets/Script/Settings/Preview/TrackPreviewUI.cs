using UnityEngine;
using UnityEngine.UI;

namespace YARG.Settings.Preview
{
    public class TrackPreviewUI : MonoBehaviour
    {
        [SerializeField]
        private Dropdown _instrumentSelector;
        [SerializeField]
        private Dropdown _displayOptionsSelector;

        private void OnEnable()
        {
            _instrumentSelector.onValueChanged.AddListener(OnInstrumentSelectorChanged);
            _displayOptionsSelector.onValueChanged.AddListener(OnDisplayOptionsSelectorChanged);
        }

        private void OnDisable()
        {
            _instrumentSelector.onValueChanged.RemoveListener(OnInstrumentSelectorChanged);
            _displayOptionsSelector.onValueChanged.RemoveListener(OnDisplayOptionsSelectorChanged);
        }

        private void OnInstrumentSelectorChanged(int index)
        {
            // SettingsManager.PreviewOptions.Instrument = (Instrument)index;
        }

        private void OnDisplayOptionsSelectorChanged(int index)
        {
            // SettingsManager.PreviewOptions.DisplayOptions = (DisplayOptions)index;
        }

        public void RequestRefresh()
        {
            var previewOptions = SettingsManager.PreviewOptions;
        }
    }
}