using YARG.Challenges;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Challenge
{

    public abstract class ViewType : BaseViewType
    {

        public abstract bool UseFullContainer { get; }

        public virtual void ViewClick()
        {

        }

        public virtual IChallenge GetChallenge()
        {
            return null;
        }

    }
}