namespace YARG.Challenges
{
    public class StarsChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.Stars;

        public override float GetActualPerformance()
        {
            return Player.Stars;
        }

        public override bool CheckForPass()
        {
            if (!Passed && Player.Stars >= Difficulties[Player.Player.Profile.CurrentDifficulty].Threshold)
            {
                Passed = true;
                return true;
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