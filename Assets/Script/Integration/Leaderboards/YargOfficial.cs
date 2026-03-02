using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Core.Chart;
using YARG.Core.Logging;
using YARG.Core.Replays;
using YARG.Core.Song;
using YARG.Song;

namespace YARG.Integration.Leaderboards
{
    public class YargOfficial : YargSpy
    {
#if UNITY_EDITOR || YARG_TEST_BUILD || YARG_NIGHTLY_BUILD
        private static  string OFFICIAL_LEADERBOARD_NAME = "YARG Official (dev/nightly)";
        private static  string OFFICIAL_LEADERBOARD_API  = "https://yargerboard-api.ulna.net/";
        public override string WebUri => "https://yargerboard.ulna.net/";
#else
        private static string OFFICIAL_LEADERBOARD_NAME = "YARG Official";
        private static string OFFICIAL_LEADERBOARD_API  = "https://leaderboard-api.yarg.in/";
        public override string WebUri => "https://leaderboard.yarg.in/";
#endif

        public override string ApiUri => OFFICIAL_LEADERBOARD_API;
        public override bool AllowSongUpload => false;

        public YargOfficial(string name, string uri) : base(OFFICIAL_LEADERBOARD_NAME, OFFICIAL_LEADERBOARD_API)
        {

        }

        public override async UniTask<UploadResponse> SubmitScore(ReplayInfo info)
        {
            // Verify that the song source is yarg, yargdlc, or yarn
            if (!SongContainer.SongsByHash.TryGetValue(info.SongChecksum, out var songInfo) || songInfo.Count < 1)
            {
                return UploadResponse.InvalidSong;
            }

            var sourceString = songInfo[0].Source.ToString();
            if (sourceString is not ("yarg" or "yargdlc" or "yarn"))
            {
                return UploadResponse.NotAllowed;
            }

            return await base.SubmitScore(info);
        }
    }
}