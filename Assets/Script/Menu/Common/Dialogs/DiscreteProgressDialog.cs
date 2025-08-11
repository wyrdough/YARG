using TMPro;
using UnityEngine;
using YARG.Menu.Data;
using YARG.Menu.Navigation;

namespace YARG.Menu.Dialogs
{
    /// <summary>
    /// A dialog that shows a progress bar and a message
    /// Warning: The caller must call SetSteps before Initialize
    /// </summary>
    public class DiscreteProgressDialog : Dialog
    {
        [SerializeField]
        protected DiscreteProgressDisplay _progressDisplay;

        [field: Space]
        [field: SerializeField]
        public TextMeshProUGUI Message { get; private set; }

        private int _totalSteps = 2;

        public override void Initialize()
        {
            _progressDisplay.Initialize(_totalSteps);
            base.Initialize();
        }

        public void SetSteps(int steps)
        {
            _totalSteps = steps;
        }

        public void ProgressToNextStep()
        {
            _progressDisplay.ProgressToNextStep();
        }

        public override void ClearDialog()
        {
            base.ClearDialog();

            Message.text = null;
            Message.color = MenuData.Colors.BrightText;
        }
    }
}