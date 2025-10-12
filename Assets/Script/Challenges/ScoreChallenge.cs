namespace YARG.Challenges
{
    public class ScoreChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Score;

        public override float GetActualPerformance()
        {
            return Player.Score;
        }

        public override bool CheckForPass()
        {
            if (!Passed && Player.Score >= Difficulties[Player.Player.Profile.CurrentDifficulty].Threshold)
            {
                Passed = true;
                return true;
            }

            return false;
        }

        public override IChallenge Clone(IChallenge other)
        {
            var clone = new ScoreChallenge
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