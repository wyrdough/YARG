using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YARG.Challenges;

namespace YARG.Helpers
{
    public class JsonChallengeConverter : JsonConverter<IChallenge>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IChallenge value, JsonSerializer serializer)
        {
            throw new System.NotImplementedException("This converter should only be used for reading");
        }

        public override IChallenge ReadJson(JsonReader reader, Type objectType, IChallenge existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);

            var typeToken = jObject["Type"];

            if (typeToken == null)
            {
                throw new JsonSerializationException("Missing challenge type");
            }

            ChallengeType challengeType;
            if (typeToken.Type == JTokenType.Integer)
            {
                var challengeInt = typeToken.Value<int>();
                if (!Enum.IsDefined(typeof(ChallengeType), challengeInt))
                {
                    throw new JsonSerializationException($"Invalid challenge type value {challengeInt}");
                }
                challengeType = (ChallengeType) challengeInt;
            }
            else if (typeToken.Type == JTokenType.String)
            {
                var challengeString = typeToken.Value<string>();
                if (!Enum.TryParse(challengeString, true, out challengeType))
                {
                    throw new JsonSerializationException($"Invalid challenge type value {challengeString}");
                }
            }
            else
            {
                throw new JsonSerializationException("Challenge type must be an integer or string");
            }

            IChallenge challenge = challengeType switch
            {
                ChallengeType.Score     => new ScoreChallenge(),
                ChallengeType.Stars     => new StarsChallenge(),
                ChallengeType.Percent   => new PercentChallenge(),
                ChallengeType.Streak    => new StreakChallenge(),
                ChallengeType.FullCombo => new FullComboChallenge(),
                _                       => throw new JsonSerializationException("Unsupported challenge type")
            };

            serializer.Populate(jObject.CreateReader(), challenge);
            return challenge;
        }
    }
}