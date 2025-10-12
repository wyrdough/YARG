using System.Collections.Generic;
using YARG.Core;

namespace YARG.Challenges
{
    public class PercentChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Percent;

        public override float GetActualPerformance()
        {
            return (float) Player.NotesHit / Player.TotalNotes;
        }

        public override bool CheckForPass()
        {
            if (!Passed && GetActualPerformance() >= Difficulties[Player.Player.Profile.CurrentDifficulty].Threshold)
            {
                Passed = true;
                return true;
            }

            return false;
        }

        public override IChallenge Clone(IChallenge other)
        {
            var clone = new PercentChallenge
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

            return clone;
        }
    }
}