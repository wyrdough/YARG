using System.Collections.Generic;
using YARG.Core;

namespace YARG.Challenges
{
    public class StarsChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Stars;

        public override float GetActualPerformance()
        {
            return Player.Stars;
        }

        public override bool CheckForPass(int songIndex)
        {
            if (!base.CheckForPass(songIndex))
            {
                return false;
            }

            if (!PlaylistPassed[songIndex] && Player.Stars >= Difficulties[Player.Player.Profile.CurrentDifficulty].Threshold)
            {
                PlaylistPassed[songIndex] = true;
                return Passed;
            }

            return false;
        }

        public override IChallenge Clone(IChallenge other)
        {
            var clone = new StarsChallenge
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