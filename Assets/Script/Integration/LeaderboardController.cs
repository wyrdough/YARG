using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Core.Song;
using YARG.Integration.Leaderboards;
using YARG.Player;

namespace YARG.Integration
{
    public struct Leaderboard : IEquatable<Leaderboard>
    {
        public int          Order;
        public ILeaderboard LeaderboardInstance;
        public YargPerson   Person;

        // Override Equals to check Person and LeaderboardInstance.ApiUri combo for efficiency
        public bool Equals(Leaderboard other)
        {
            return Person == other.Person && LeaderboardInstance.ApiUri == other.LeaderboardInstance.ApiUri;
        }

        public override bool Equals(object obj) => obj is Leaderboard other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(LeaderboardInstance.ApiUri, Person);
    }

    public class LeaderboardController : MonoSingleton<LeaderboardController>
    {
        private readonly List<Leaderboard> _leaderboards = new();

        public void AddLeaderboard(Leaderboard leaderboard)
        {
            if(!_leaderboards.Contains(leaderboard))
            {
                _leaderboards.Add(leaderboard);
            }
        }

        public void RemoveLeaderboard(Leaderboard leaderboard)
        {
            if(_leaderboards.Contains(leaderboard))
            {
                leaderboard.LeaderboardInstance.Logout();
                _leaderboards.Remove(leaderboard);
            }
        }

        public async UniTask<bool> GetLeaderboardInfo(string uri)
        {
            using UnityWebRequest request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("User-Agent", "YARG");
            request.SetRequestHeader("Accept", "application/json");
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                return false;
            }

            var json = request.downloadHandler.text;

            // Do something with this, but idk what yet

            return false;
        }

        public void GetRankingsForChart(Leaderboard leaderboard, HashWrapper songHash)
        {

        }

        public void GetAllRankingsForUser(Leaderboard leaderboard)
        {

        }
    }
}