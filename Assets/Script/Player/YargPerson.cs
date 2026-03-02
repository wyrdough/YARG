using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using YARG.Core.Game;
using YARG.Integration;

namespace YARG.Player
{
    // TODO: This is a bad name, it should be changed later when we're further along in the profile rework

    /// <summary>
    /// Contains information about the person playing and references profiles (instrument profiles) "owned" by that person
    /// </summary>
    public class YargPerson
    {
        public string Name;

        // We serialize the Guids only and rebuild the references on deserialize
        public List<Guid> ProfileIds = new List<Guid>();

        [NonSerialized]
        public List<YargProfile> Profiles = new List<YargProfile>();

        public List<Leaderboard> Leaderboards = new();

        public Leaderboard? DefaultLeaderboard => Leaderboards.OrderBy(l => l.Order).FirstOrDefault();

        public void AddProfile(YargProfile profile)
        {
            if (!Profiles.Contains(profile))
            {
                Profiles.Add(profile);
                ProfileIds.Add(profile.Id);
            }
        }

        public void RemoveProfile(YargProfile profile)
        {
            if (Profiles.Contains(profile))
            {
                Profiles.Remove(profile);
                ProfileIds.Remove(profile.Id);
            }
        }

        public async void AddLeaderboard(string uri, string key)
        {
            // TODO: Show something telling the user to wait a sec while we gather the leaderboard info
            await LeaderboardController.Instance.GetLeaderboardInfo(uri);

        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            ProfileIds = Profiles.Select(p => p.Id).ToList();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Profiles = ProfileIds.Select(PlayerContainer.GetProfileById).ToList();
        }
    }
}