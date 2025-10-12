namespace YARG.Challenges
{
    public class FullComboChallenge : Challenge
    {
        public override ChallengeType Type => ChallengeType.FullCombo;

        public override float GetActualPerformance()
        {
            // Returns 0f until the player has actually completed the song
            return Player.TotalNotes == Player.NotesHit && Player.IsFc ? 1f : 0f;
        }

        public override bool CheckForPass()
        {
            if (!Passed && Player.TotalNotes == Player.NotesHit && Player.IsFc)
            {
                Passed = true;
                return true;
            }

            return false;
        }

        public override IChallenge Clone(IChallenge other)
        {
            var clone = new FullComboChallenge
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