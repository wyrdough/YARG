using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Core.Logging;
using YARG.Core.Replays;
using YARG.Core.Song;

namespace YARG.Integration.Leaderboards
{
    public class YargSpy : ILeaderboard
    {
        public         string Name     { get; }
        public         string Username { get; set; }
        public         string UserId   { get; private set; }
        public         string LastError { get; private set; }

        public virtual string ApiUri   { get; }
        public virtual string WebUri   => "https://yargspy.com";

        public virtual bool IsLoggedIn      => !string.IsNullOrEmpty(_token);
        public virtual bool AllowSongUpload => true;

        private string _token;

        public YargSpy(string name, string uri)
        {
            Name = name;
            ApiUri = uri;
        }

        // TODO: This could be set up to allow uploading official charts...
        //  also, we should really be returning the song ID so we can follow up with a request for rankings
        public virtual async UniTask<UploadResponse> SubmitScore(ReplayInfo info)
        {
            var submissionUri = $"{ApiUri}/replay/register";
            var submissionType = "replayOnly";

            var filename = info.ReplayName;
            if (!File.Exists(info.FilePath))
            {
                return UploadResponse.ReplayNotFound;
            }

            var replayFile = await File.ReadAllBytesAsync(info.FilePath);

            var form = new WWWForm();
            form.AddField("reqType", submissionType);
            form.AddBinaryData("replayFile", replayFile, filename, "application/octet-stream");

            using UnityWebRequest request = UnityWebRequest.Post(submissionUri, form);
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Authorization", $"Bearer {_token}");
            request.SetRequestHeader("Accept", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                YargLogger.LogFormatError<string,string>("Failed to submit score to {0}: {1}", Name, request.error);
                return UploadResponse.ConnectionError;
            }

            var response = JsonUtility.FromJson<ReplayUploadResponse>(request.downloadHandler.text);
            if (response.statusCode != 201)
            {
                YargLogger.LogFormatError<string,string>("Failed to submit score to {0}: {1}", Name, response.message);
                return UploadResponseFromCode(response.code);
            }

            return UploadResponse.Success;
        }

        public virtual async UniTask<List<LeaderboardScore>> GetRecentScores()
        {
            var scores = new List<LeaderboardScore>();

            var uri = $"{ApiUri}/user/scores?id={UserId}&limit=10&page=1";

            using var request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_token}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                YargLogger.LogFormatError<string,string>("Failed to get recent scores from {0}: {1}", Name, request.error);
                return scores;
            }

            var response = JsonConvert.DeserializeObject<ScoresResponse>(request.downloadHandler.text);
            if (response.statusCode != 200)
            {
                YargLogger.LogFormatError<string,string>("Failed to get recent scores from {0}: {1}", Name, response.message);
                return scores;
            }

            var rank = 1;
            foreach (var entry in response.entries)
            {
                var hash = HashWrapper.FromString(entry.replayFileHash.ToUpperInvariant());
                // Turn this into useful data
                scores.Add(new LeaderboardScore
                {
                    Score = entry.score,
                    Stars = entry.stars,
                    Accuracy = entry.percent,
                    Song = hash,
                    Rank = 0,
                    MaxCombo = entry.maxCombo,
                    Username = Username,
                    Date = entry.createdAt,
                });

                rank++;
            }

            return scores;
        }

        public virtual List<LeaderboardScore> GetScores(SongEntry chart)
        {
            return new List<LeaderboardScore>();
        }

        public virtual List<LeaderboardScore> GetTopScores()
        {
            return new List<LeaderboardScore>();
        }

        public virtual async UniTask<bool> Login(string username, string password)
        {
            var loginUri = $"{ApiUri}/user/login";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            var loginRequest = new LoginRequest
            {
                username = username,
                password = password
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            using var request = UnityWebRequest.Post(loginUri, json, "application/json");
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Accept", "application/json");
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                YargLogger.LogFormatError<string,string>("Failed to login to {0}: {1}", Name, request.error);
                return false;
            }

            try
            {
                var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);

                if (response.statusCode != 200)
                {
                    LastError = response.message;
                    return false;
                }

                _token = response.token;

                var userId = await GetUserId();
                UserId = userId;

                return true;
            }
            catch (JsonException e)
            {
                YargLogger.LogException(e, "Failed to parse login response.");
            }

            return false;
        }

        public virtual bool Logout()
        {
            // Just clear the token since YargSpy doesn't have a logout endpoint
            _token = null;
            return true;
        }

        protected virtual async UniTask<string> GetSongId(string hash)
        {
            var lowerHash = hash.ToLowerInvariant();
            var uri = $"{ApiUri}/song/hashToId${lowerHash}";

            var request = new UnityWebRequest(uri, "GET");
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_token}");
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                YargLogger.LogFormatError<string,string>("Failed to get song ID for {0}: {1}", Name, request.error);
                return null;
            }

            var response = JsonConvert.DeserializeObject<HashToIdResponse>(request.downloadHandler.text);
            if (response.statusCode != 200)
            {
                YargLogger.LogFormatError<string,string>("Failed to get song ID for {0}: {1}", Name, response.message);
            }

            return response.id;
        }

        protected async UniTask<string> GetUserId()
        {
            if (string.IsNullOrEmpty(_token))
            {
                return null;
            }

            var uri = $"{ApiUri}/user/profile";

            var request = new UnityWebRequest(uri, "GET");
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {_token}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                YargLogger.LogFormatError<string,string>("Failed to get user ID for {0}: {1}", Name, request.error);
                return null;
            }

            var response = JsonConvert.DeserializeObject<UserIdResponse>(request.downloadHandler.text);
            if (response.statusCode != 200)
            {
                YargLogger.LogFormatError<string,string>("Failed to get user ID for {0}: {1}", Name, response.message);
                return null;
            }

            UserId = response.user._id;
            return UserId;
        }

        protected UploadResponse UploadResponseFromCode(string code)
        {
            return code switch
            {
                "err_replay_duplicated_score" => UploadResponse.ReplayAlreadySubmitted,
                "err_replay_no_valid_players" => UploadResponse.UnsupportedReplay,
                "err_invalid_auth_format"     => UploadResponse.NotLoggedIn,
                "err_auth_required"           => UploadResponse.NotLoggedIn,
                _                             => UploadResponse.UnknownFailure,
            };
        }

        // ReSharper disable InconsistentNaming
        private class LoginRequest
        {
            public string username;
            public string password;
        }

        private class LoginResponse
        {
            public int    statusCode;
            public string statusName;
            public string statusFullName;
            public string code;
            public string message;
            public string token;
        }

        protected class ReplayUploadResponse
        {
            public int    statusCode;
            public string statusName;
            public string statusFullName;
            public string code;
            public string message;
            public string song;
        }

        protected class ScoresResponse
        {
            public int                 statusCode;
            public string              statusName;
            public string              statusFullName;
            public string              code;
            public string              message;
            public int                 totalEntries;
            public int                 totalPages;
            public int                 page;
            public int                 limit;
            public YargSpyScoreEntry[] entries;
        }

        protected class YargSpySong
        {
            public string _id;
            public string name;
            public string artist;
            public string charter;
            public string chartFileHash;
        }

        protected class YargSpyLeaderboardEntry
        {
            public YargSpyScoreEntry[] childrenScores;
            public DateTime            createdAt;
            public bool                hidden;
            public int                 instrument;
            public int[]               modifiers;
            public string              replayFileHash;
            public string              replayPath;
            public int                 score;
            public string              song;
            public int                 songSpeed;
            public int                 stars;
            public YargSpyUploader     uploader;
            public int                 version;
            public int                 __v;
            public int                 _id;
        }

        protected class YargSpyScoreEntry
        {
            public string                  _id;
            public YargSpySong             song;
            public string                  uploader;
            public string                  replayPath;
            public string                  replayFileHash;
            public YargSpyScoreEntry[]     childrenScores;
            public int                     version;
            public bool                    hidden;
            public int                     instrument;
            public int                     gamemode;
            public int                     difficulty;
            public int                     engine;
            public int[]                   modifiers;
            public int                     songSpeed;
            public string                  profileName;
            public int                     score;
            public int                     stars;
            public int                     notesHit;
            public int                     maxCombo;
            public int                     starPowerPhrasesHit;
            public int                     starPowerActivationCount;
            public int                     soloBonuses;
            public DateTime                createdAt;
            public float                   percent;
            public int                     overhits;
            public int                     ghostInputs;
            public int                     sustainScore;
            public int                     __v;
        }

        protected class UserIdResponse
        {
            public int         statusCode;
            public string      statusName;
            public string      statusFullName;
            public string      code;
            public string      message;
            public YargSpyUser user;
        }

        protected class YargSpyUser
        {
            public string _id;
            public string username;
            public string email;
            public string emailVerified;
            public string active;
            public string admin;
            public string country;
            public string createdAt;
            public string updatedAt;
        }

        protected class YargSpyUploader
        {
            public string username;
            public string country;
        }

        protected class HashToIdResponse
        {
            public int    statusCode;
            public string statusName;
            public string statusFullName;
            public string code;
            public string message;
            public string id;
        }

        // ReSharper restore InconsistentNaming
    }
}