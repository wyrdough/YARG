using YARG.Localization;
using YARG.Menu.Data;
using YARG.Menu.Dialogs;
using YARG.Menu.Persistent;
using YARG.Settings;

namespace YARG.Menu.Main
{
    public class Onboarding
    {
        private readonly MainMenu                _mainMenu;
        private          OnboardingProfileDialog _onboardingDialog;

        public Onboarding(MainMenu menu)
        {
            _mainMenu = menu;
        }

        public void ShowOnboardingFlow()
        {
            _onboardingDialog = DialogManager.Instance.ShowOnboardingMessage(
                "Menu.Dialog.FirstTimePlayer",
                () =>
                {
                    SettingsManager.Settings.FirstTimeDialogShown = true;
                    SettingsManager.SaveSettings();
                }, ShowOnboardingStepTwo);
            _onboardingDialog.ClearButtons();
            _onboardingDialog.AddDialogButton(
                Localize.Key("Menu.Dialog.FirstTimePlayer.Skip"),
                MenuData.Colors.CancelButton,
                () =>
                {
                    DialogManager.Instance.ClearDialog();
                    _onboardingDialog = null;
                }
            );
            _onboardingDialog.AddDialogButton(
                Localize.Key("Menu.Dialog.FirstTimePlayer.Confirm"),
                MenuData.Colors.ConfirmButton,
                ShowOnboardingStepTwo
            );
        }

        private async void ShowOnboardingStepTwo()
        {
            _onboardingDialog.StepTwo();
            await _onboardingDialog.WaitUntilClosed();
            DialogManager.Instance.ClearDialog();
            _onboardingDialog = null;
        }

        private void ShowProfileMenu()
        {
            _mainMenu.Profiles();
        }
    }
}