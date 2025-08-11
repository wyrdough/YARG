namespace YARG.Menu.Dialogs
{
    public class InputBindingDialog : ImageDialog
    {

        protected override void OnEnable()
        {
            // TODO: Figure out how to get the button presses even when they aren't yet bound
            base.OnEnable();
        }

        protected override void OnBeforeClose()
        {
            // TODO: Undo whatever you did to get the button presses
            base.OnBeforeClose();
        }

    }
}