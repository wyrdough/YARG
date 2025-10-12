using System;
using SQLite;
using YARG.Challenges;
using YARG.Core;
using YARG.Core.Song;

namespace YARG.Scores
{
    [Table("Challenges")]
    public class ChallengeRecord
    {
        // The guid of the challenge
        [Indexed]
        public Guid Id { get; set; }
        // The PlayerScoreRecord that satisfied this challenge (may be null for section challenges)
        // This would be a foreign key if the version of sqlite-net we are using supported FKs...
        // [ForeignKey(typeof(PlayerScoreRecord))]
        [Indexed]
        public Guid PlayerScoreId { get; set; }
        [Indexed]
        public Guid PlayerId { get; set; }
        [Indexed]
        public DateTime DateCompleted { get; set; }

        public ChallengeType Type                 { get; set; }
        public string        ChallengeName        { get; set; }
        public string        ChallengeAuthor      { get; set; }
        public string        ChallengeDescription { get; set; }
        public Difficulty    ChallengeDifficulty  { get; set; }
        public Instrument    ChallengeInstrument  { get; set; }
        public float         ChallengeThreshold   { get; set; }
        public float         Achieved             { get; set; }
        public bool          IsFc                 { get; set; }
        public float         MinimumSpeed         { get; set; }
        public float         ActualSpeed          { get; set; }

        public byte[]      SongHash    { get; set; }
        public string      SongName    { get; set; }
        public string      SongArtist  { get; set; }
        public string      SongCharter { get; set; }
    }
}