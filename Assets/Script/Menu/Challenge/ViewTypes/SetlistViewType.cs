using UnityEngine;
using YARG.Challenges;
using YARG.Player;

namespace YARG.Menu.Challenge
{
    public class SetlistViewType : ChallengeViewType
    {
        public SetlistViewType(IChallenge challenge, YargPlayer player) : base(challenge, player)
        {

        }

        public override void ViewClick()
        {
            // Launch into song in the normal way, but single player
            GlobalVariables.State = PersistentState.Default;
            GlobalVariables.State.PlayingAShow = true;
            GlobalVariables.State.ShowSongs = SongEntries;
            GlobalVariables.State.CurrentSong = SongEntries[0];
            GlobalVariables.State.ChallengeMode = true;
            GlobalVariables.State.ChallengeProfile = Player;

            GlobalVariables.State.ChallengeRequiredSpeed = Mathf.Max(GlobalVariables.State.SongSpeed, Challenge.MinimumSpeed / 100f);

            GlobalVariables.Instance.LoadScene(SceneIndex.Gameplay);
        }
    }
}