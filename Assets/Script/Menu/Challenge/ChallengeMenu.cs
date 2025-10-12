using System.Collections.Generic;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Challenge
{
    public class ChallengeMenu : ListMenu<ViewType, ChallengeView>
    {
        protected override int ExtraListViewPadding => 10;

        protected override List<ViewType> CreateViewList()
        {
            throw new System.NotImplementedException();
        }
    }
}