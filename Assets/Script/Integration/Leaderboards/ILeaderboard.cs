using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YARG.Core.Replays;
using YARG.Core.Song;

namespace YARG.Integration.Leaderboards
{
    public struct LeaderboardScore
    {
        public HashWrapper Song;
        public string      Username;
        public int         Score;
        public int         Rank;
        public int         MaxCombo;
        public int         Stars;
        public float       Accuracy;
        public DateTime    Date;
    }

    public enum UploadResponse
    {
        Success,
        NotLoggedIn,
        InvalidSong,
        InvalidReplay,
        UnsupportedReplay,
        ReplayNotFound,
        ReplayAlreadySubmitted,
        NotAllowed,
        ConnectionError,
        UnknownFailure
    }

    public interface ILeaderboard
    {
        string Name            { get; }
        string ApiUri          { get; }
        string WebUri          { get; }
        string Username        { get; set; }
        string LastError       { get; }
        bool   IsLoggedIn      { get; }
        bool   AllowSongUpload { get; }

        UniTask<UploadResponse> SubmitScore(ReplayInfo info);

        List<LeaderboardScore> GetScores(SongEntry song);
        List<LeaderboardScore> GetTopScores();

        UniTask<bool> Login(string username, string password);
        bool Logout();
    }
}