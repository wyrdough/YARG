namespace YARG.Challenges
{
    public class StreakChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Streak;

        public override float GetActualPerformance()
        {
            return Player.BaseStats.MaxCombo;
        }

        public override bool CheckForPass()
        {
            var currentDiff = Difficulties[Player.Player.Profile.CurrentDifficulty];

            if (!Passed && Player.BaseStats.MaxCombo >= currentDiff.Threshold)
            {
                Passed = true;
                return true;
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
                Difficulties = other.Difficulties,
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