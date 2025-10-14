using System.Collections.Generic;
using UnityEngine;
using YARG.Challenges;
using YARG.Core.Song;
using YARG.Player;
using YARG.Song;

namespace YARG.Menu.Challenge
{
    public class ChallengeViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Normal;

        public override bool UseFullContainer => true;

        protected IChallenge Challenge;

        protected List<SongEntry> SongEntries = new();

        protected YargPlayer Player;

        public ChallengeViewType(IChallenge challenge, YargPlayer player)
        {
            Challenge = challenge;
            Player = player;

            foreach (var hash in challenge.SongHashes)
            {
                if (SongContainer.SongsByHash.TryGetValue(hash, out var songs))
                {
                    SongEntries.Add(songs[0]);
                }
            }

            challenge.LoadDataForPlayer(Player);
        }

        public override string GetPrimaryText(bool selected)
        {
            return FormatAs(Challenge.Name, TextType.Primary, selected);
        }

        public override string GetSecondaryText(bool selected)
        {
            return FormatAs(Challenge.Description, TextType.Secondary, selected);
        }

        public override void ViewClick()
        {
            GlobalVariables.State = PersistentState.Default;
            GlobalVariables.State.ChallengeMode = true;
            GlobalVariables.State.ChallengeProfile = Player;
            GlobalVariables.State.CurrentChallenge = Challenge;
            GlobalVariables.State.CurrentSong = SongEntries[0];

            if (Challenge.Length == ChallengeLength.Setlist)
            {
                GlobalVariables.State.PlayingAShow = true;
                GlobalVariables.State.ShowSongs = SongEntries;
            }

            if (Challenge.Length == ChallengeLength.Section)
            {
                GlobalVariables.State.IsPractice = true;
                GlobalVariables.State.PracticeSection = Challenge.Section;
            }

            GlobalVariables.State.ChallengeRequiredSpeed = Mathf.Max(GlobalVariables.State.SongSpeed, Challenge.MinimumSpeed / 100.0f);

            MenuManager.Instance.PushMenu(MenuManager.Menu.DifficultySelect);
        }

        public override IChallenge GetChallenge()
        {
            return Challenge;
        }
    }
}