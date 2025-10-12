using YARG.Menu.ListMenu;

namespace YARG.Menu.Challenge
{

    public abstract class ViewType : BaseViewType
    {

        public abstract bool UseFullContainer { get; }

        public virtual void ViewClick()
        {

        }

    }
}