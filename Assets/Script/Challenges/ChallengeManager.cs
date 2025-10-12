using System.Collections.Generic;
using YARG.Core;
using YARG.Core.Chart;
using YARG.Core.Song;
using YARG.Gameplay;
using YARG.Gameplay.Player;
using YARG.Menu.Persistent;
using YARG.Player;
using YARG.Scores;

namespace YARG.Challenges
{
    public class ChallengeManager : GameplayBehaviour
    {
        private readonly List<IChallenge>  _activeChallenges = new();
        private          SongEntry        _song;


        protected override void GameplayAwake()
        {
            // Check if the song we are playing has one or more challenges
            // we can satisfy
            _song = GameManager.Song;
            // We multiply by 100 because it's easier for challenge makers to grok when expressed as a percentage
            var speed = GameManager.SongSpeed * 100;

            foreach (var challenge in ChallengeContainer.Challenges)
            {
                // Filter out challenges that are not currently active
                if (!challenge.IsActive || speed < challenge.MinimumSpeed)
                {
                    continue;
                }

                // Because the below are unimplemented, skip if challenge length is not song
                // TODO: Actually implement section and playlist challenges and remove this check
                if (challenge.Length != ChallengeLength.Song)
                {
                    continue;
                }

                // Filter out challenges that are not valid for this game mode (aka, no section challenges except in practice mode)
                if (challenge.Length == ChallengeLength.Section && !GameManager.IsPractice)
                {
                    continue;
                }

                if (challenge.Length == ChallengeLength.Playlist && !GlobalVariables.State.PlayingAShow)
                {
                    continue;
                }

                var hashWrappers = challenge.SongHashes;
                foreach (var hashWrapper in hashWrappers)
                {
                    if (_song.Hash.Equals(hashWrapper))
                    {
                        foreach (var player in GameManager.Players)
                        {
                            var validInstrument = false;
                            foreach (var instrument in challenge.Instruments)
                            {
                                if (instrument.Equals(player.Player.Profile.CurrentInstrument))
                                {
                                    validInstrument = true;
                                    break;
                                }
                            }

                            var completed = ScoreContainer.GetChallengeCompletion(challenge.ID, player.Player, _song);

                            if (ScoreContainer.GetChallengeCompletion(challenge.ID, player.Player, _song) != null)
                            {
                                continue;
                            }

                            if (validInstrument &&
                                !challenge.HasPassedDifficulty(player.Player.Profile.CurrentDifficulty))
                            {
                                var clone = challenge.Clone(challenge);
                                clone.Player = player;
                                _activeChallenges.Add(clone);
                            }
                        }
                    }
                }
            }
        }

        // We could check only at the end of the song in OnSongEnding, but it would be nicer
        // to pop the notification as soon as it is met, so an update function it is...
        protected void Update()
        {
            foreach (var challenge in _activeChallenges)
            {
                if (challenge.CheckForPass())
                {
                    ToastManager.ToastSuccess($"Congratulations! You passed the {challenge.Name} challenge!");
                }
            }
        }

        protected override void OnSongEnding()
        {
            // The challenge objects know their own pass state, so we can safely instruct them to save without
            // checking if there was a pass or not
            foreach (var challenge in _activeChallenges)
            {
                challenge.SavePass(_song);
            }
        }
    }
}