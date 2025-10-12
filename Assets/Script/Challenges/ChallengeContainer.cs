using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YARG.Core;
using YARG.Core.Logging;
using YARG.Core.Song;
using YARG.Core.Utility;
using YARG.Helpers;

namespace YARG.Challenges
{
    public static class ChallengeContainer
    {
        public static readonly List<IChallenge> Challenges = new();

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>
            {
                new JsonHashWrapperConverter(),
                new StringEnumConverter(),
                new JsonChallengeConverter()
            }
        };

        public static string ChallengeDirectory { get; private set; }

        public static void Initialize()
        {
            ChallengeDirectory = Path.Combine(PathHelper.PersistentDataPath, "challenges");

            if (!Directory.Exists(ChallengeDirectory))
            {
                Directory.CreateDirectory(ChallengeDirectory);
            }

            // Temporary: Make a test challenge when we initialize
            // MakeTestChallenge();

            // Load any challenges we find in the challenges folder
            foreach (var file in Directory.GetFiles(ChallengeDirectory))
            {
                if (!file.EndsWith(".json"))
                {
                    continue;
                }

                var challenge = LoadChallenge(file);
                Challenges.Add(challenge);
            }
        }

        private static IChallenge LoadChallenge(string filePath)
        {
            try
            {
                var text = File.ReadAllText(filePath);
                var challenge = JsonConvert.DeserializeObject<IChallenge>(text, JsonSettings);

                // Since the state of the challenge depends on the profile/instrument, we can't determine
                // here whether it has yet been satisifed. The menu will have to do that...

                return challenge;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Failed to load challenge file");
            }

            return null;
        }

        public static void SaveChallenge(IChallenge challenge)
        {
            // TODO: This is not OK for long term use, it only works because we are writing out
            //  a known test challenge that we know to have a reasonable name.
            var path = Path.Combine(ChallengeDirectory, $"{challenge.Name}.json");

            try
            {
                var text = JsonConvert.SerializeObject(challenge, JsonSettings);
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Failed to save challenge file");
            }
        }

        private static void MakeTestChallenge()
        {
            List<Difficulty> difficulties = new() { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert};
            var challenge = new PercentChallenge
            {
                ID = Guid.NewGuid(),
                Name = "Frere Jacques Challenge",
                Description = "Complete the song with at least 90% accuracy",
                Author = "YARG",
                Section = "",
                Instruments = new Instrument[] { Instrument.FiveFretGuitar, Instrument.FiveFretBass },
                Difficulties = new Dictionary<Difficulty, ChallengeDifficulty>(),
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(15),
                Length = ChallengeLength.Song,
                // Adamic - All Of A Sudden
                // SongHashes = new HashWrapper[] { HashWrapper.FromString("C1AFEE7AB024BA7BFED64D0869D08F53A57EE1C1") },
                // frere jacques
                SongHashes = new HashWrapper[] { HashWrapper.FromString("9849D41914E7E737C22C2DA4872CE8A0043E16F7") },
            };

            foreach (var difficulty in difficulties)
            {
                challenge.Difficulties.Add(difficulty, new ChallengeDifficulty { Difficulty = difficulty, Threshold = 0.9f });
            }

            SaveChallenge(challenge);
        }
    }
}