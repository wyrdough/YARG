using System.Collections.Generic;
using System.Linq;
using YARG.Challenges;
using YARG.Core.Input;
using YARG.Core.Song;
using YARG.Menu.ListMenu;
using YARG.Menu.Navigation;
using YARG.Player;
using YARG.Scores;

namespace YARG.Menu.Challenge
{
    public class ChallengeMenu : ListMenu<ViewType, ChallengeView>
    {
        protected override int ExtraListViewPadding => 10;

        private void OnEnable()
        {
            Navigator.Instance.PushScheme(new NavigationScheme(new()
                {
                    new NavigationScheme.Entry(MenuAction.Up, "Menu.Common.Up",
                        ctx => {
                            SetWrapAroundState(!ctx.IsRepeat);
                            SelectedIndex--;
                        }),
                    new NavigationScheme.Entry(MenuAction.Down, "Menu.Common.Down",
                        ctx => {
                            SetWrapAroundState(!ctx.IsRepeat);
                            SelectedIndex++;
                        }),
                    new NavigationScheme.Entry(MenuAction.Green, "Menu.Common.Confirm",
                        () => CurrentSelection?.ViewClick()),
                    new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Back),
                }, false
                ));
        }

        protected override List<ViewType> CreateViewList()
        {
            var list = new List<ViewType>();
            var allChallenges = ChallengeContainer.Challenges;

            // TODO: Cache this
            var completedChallenges = new List<IChallenge>();
            var uncompletedChallenges = new List<IChallenge>();

            // Get the first non-bot player in PlayerContainer.Players
            var player = PlayerContainer.Players.FirstOrDefault(p => !p.Profile.IsBot);

            if (player is null)
            {
                // TODO: Show error message as is done in music library
                return list;
            }

            // TODO: We actually need to not rely on ChallengeContainer to have all the completed challenges,
            //  as the JSON the IChallenge objects are built from may no longer exist for old challenges
            foreach (var challenge in allChallenges)
            {
                // TODO: This does not deal with playlist challenges correctly
                var challengeRecord = ScoreContainer.GetChallengeCompletion(challenge.ID, player, challenge.SongHashes[0]);

                if (challengeRecord is null)
                {
                    uncompletedChallenges.Add(challenge);
                }
                else
                {
                    completedChallenges.Add(challenge);
                }
            }

            // Active and uncompleted challenges
            list.Add(new CategoryViewType("Uncompleted Challenges"));

            foreach (var challenge in uncompletedChallenges)
            {
                var view = new ChallengeViewType(challenge, player);
                list.Add(view);
            }

            list.Add(new CategoryViewType("Completed Challenges"));

            foreach (var challenge in completedChallenges)
            {
                var view = new ChallengeViewType(challenge, player);
                list.Add(view);
            }

            // throw new System.NotImplementedException();
            return list;
        }

        private void Back()
        {
            MenuManager.Instance.PopMenu();
        }

        private void OnDisable()
        {
            Navigator.Instance.PopScheme();
        }
    }
}