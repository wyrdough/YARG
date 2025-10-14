using System.Collections.Generic;
using YARG.Core;

namespace YARG.Challenges
{
    public class StreakChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Streak;

        public override float GetActualPerformance()
        {
            return Player.BaseStats.MaxCombo;
        }

        public override bool CheckForPass(int songIndex)
        {
            if (!base.CheckForPass(songIndex))
            {
                return false;
            }

            var currentDiff = Difficulties[Player.Player.Profile.CurrentDifficulty];

            if (!PlaylistPassed[songIndex] && Player.BaseStats.MaxCombo >= currentDiff.Threshold)
            {
                PlaylistPassed[songIndex] = true;
                return Passed;
            }

            return false;
        }

        public override IChallenge Clone(IChallenge other)
        {
            var clone = new StreakChallenge
            {
                ID = other.ID,
                Name = other.Name,
                Author = other.Author,
                Description = other.Description,
                Length = other.Length,
                SongHashes = other.SongHashes,
                Section = other.Section,
                Instruments = other.Instruments,
                MinimumSpeed = other.MinimumSpeed,
                Difficulties = new Dictionary<Difficulty, ChallengeDifficulty>(),
                StartTime = other.StartTime,
                EndTime = other.EndTime,
            };

            // Copy the difficulties
            foreach (var difficulty in other.Difficulties)
            {
                clone.Difficulties[difficulty.Key] = ChallengeDifficulty.Clone(difficulty.Value);
            }

            clone.Initialize();
            return clone;
        }
    }
}