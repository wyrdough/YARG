using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YARG.Core;
using YARG.Core.Song;
using YARG.Gameplay;
using YARG.Gameplay.Player;
using YARG.Player;

namespace YARG.Challenges
{
    public interface IChallenge
    {
        public Guid          ID     { get; }
        public string        Name   { get; }
        public string        Author { get; }
        public string Description { get; }

        ChallengeType          Type   { get; }
        public ChallengeLength Length { get; }

        public HashWrapper[]       SongHashes   { get; }
        public string       Section      { get; }
        public Instrument[] Instruments   { get; }
        public float        MinimumSpeed { get; }

        public Dictionary<Difficulty,ChallengeDifficulty> Difficulties { get; }

        public DateTime StartTime { get; }
        public DateTime EndTime   { get; }

        // Not set or used internally, this exists to make life easier for consumers
        public BasePlayer Player { get; set; }
        public GameManager GameManager { get; set; }

        public bool Loaded { get; }

        public Guid LoadedPlayer { get; }

        public bool IsActive  => DateTime.Now >= StartTime && DateTime.Now <= EndTime;

        public bool AllPassed
        {
            get
            {
                var allPassed = true;

                foreach (var difficulty in Difficulties)
                {
                    if (!difficulty.Value.Passed)
                    {
                        allPassed = false;
                        break;
                    }
                }
                return allPassed;
            }
        }

        public bool HasPassedDifficulty(Difficulty difficulty)
        {
            if (!Difficulties.TryGetValue(difficulty, out var difficultyData))
            {
                // If we don't have a difficulty, we consider it passed
                return true;
            }

            return difficultyData.Passed;
        }

        public abstract void Initialize();

        public abstract bool CheckForPass(int songIndex);

        public abstract void SavePass(SongEntry songEntry);

        public abstract float GetActualPerformance();

        public abstract void LoadDataForPlayer(YargPlayer player, bool reload = false);

        public abstract IChallenge Clone(IChallenge other);
    }
}