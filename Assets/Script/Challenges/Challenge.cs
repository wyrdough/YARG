using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YARG.Core;
using YARG.Core.Song;
using YARG.Gameplay.Player;
using YARG.Player;
using YARG.Scores;

namespace YARG.Challenges
{
    public abstract class Challenge : IChallenge
    {
        public Guid   ID          { get; set; }
        public string Name        { get; set; }
        public string Author      { get; set; }
        public string Description { get; set; }

        public abstract ChallengeType   Type   { get; }
        public ChallengeLength Length { get; set; }

        public HashWrapper[] SongHashes   { get; set; }
        public string        Section      { get; set; }
        public Instrument[]  Instruments   { get; set; }
        public float         MinimumSpeed { get; set; } = 100.0f;

        public Dictionary<Difficulty, ChallengeDifficulty> Difficulties { get; set; }

        // Challenges are only available for a certain time
        public DateTime StartTime { get; set; }
        public DateTime EndTime   { get; set; }

        [JsonIgnore]
        // Not set or used internally, this exists to make life easier for consumers
        public BasePlayer Player { get; set; }

        // Whether we passed the challenge this time around
        [JsonIgnore]
        protected bool Passed;

        public bool HasPassedDifficulty(Difficulty difficulty)
        {
            if (!Difficulties.TryGetValue(difficulty, out var difficultyData))
            {
                // If we don't have a difficulty, we consider it passed
                return true;
            }

            return difficultyData.Passed;
        }

        public void GetPassStatus()
        {
            foreach (var difficulty in Difficulties)
            {
                // Load pass status from scores.db (challenge table will have challenge ID, player and game record id,
                // we can use game record to get difficulty/pass time/etc)
            }
        }

        public abstract float GetActualPerformance();

        public abstract bool CheckForPass();

        public virtual void SavePass(SongEntry songEntry)
        {
            var difficulty = Player.Player.Profile.CurrentDifficulty;
            var passed = Difficulties[difficulty].Passed;

            var actualAchieved = GetActualPerformance();

            // Make call to save the challenge into scores.db
            if (Passed)
            {
                ScoreContainer.RecordChallengeCompletion(new ChallengeRecord {
                    Id = ID,
                    PlayerId = Player.Player.Profile.Id,
                    DateCompleted = DateTime.Now,
                    Type = Type,
                    MinimumSpeed = MinimumSpeed,
                    ActualSpeed = (float) Player.BaseEngine.BaseParameters.SongSpeed,
                    ChallengeDifficulty = difficulty,
                    ChallengeInstrument = Player.Player.Profile.CurrentInstrument,
                    ChallengeAuthor = Author,
                    ChallengeName = Name,
                    ChallengeDescription = Description,
                    ChallengeThreshold = Difficulties[difficulty].Threshold,
                    Achieved = actualAchieved,
                    IsFc = Player.IsFc,
                    SongHash = songEntry.Hash.HashBytes,
                    SongName = songEntry.Name,
                    SongArtist = songEntry.Artist,
                    SongCharter = songEntry.Charter,
                    });
            }
        }

        public abstract IChallenge Clone(IChallenge other);

        public override string ToString()
        {
            return $"YARG Challenge: {Name} ({Author})";
        }
    }

    public class ChallengeDifficulty
    {
        public Difficulty Difficulty;
        public float      Threshold;
        [JsonIgnore]
        public bool       Passed;

        public static ChallengeDifficulty Clone(ChallengeDifficulty other)
        {
            return new ChallengeDifficulty
            {
                Difficulty = other.Difficulty,
                Threshold = other.Threshold,
                Passed = other.Passed
            };
        }
    }

    public enum ChallengeType
    {
        Score,
        Stars,
        Percent,
        Streak,
        FullCombo,
    }

    public enum ChallengeLength
    {
        Section,
        Song,
        Playlist,
    }
}