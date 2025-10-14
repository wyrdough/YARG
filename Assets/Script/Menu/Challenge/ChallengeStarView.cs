using System;
using UnityEngine;
using UnityEngine.UI;
using YARG.Challenges;
using YARG.Core;

namespace YARG.Menu.Challenge
{
    public class ChallengeStarView : MonoBehaviour
    {
        [Header("Star Graphics")]
        [SerializeField]
        private Sprite _easyStar;
        [SerializeField]
        private Sprite _mediumStar;
        [SerializeField]
        private Sprite _hardStar;
        [SerializeField]
        private Sprite _expertStar;

        [Space]
        [SerializeField]
        private Image[] _starImages;

        public void SetStars(IChallenge challenge)
        {
            foreach (var challengeDiff in challenge.Difficulties)
            {
                if (challengeDiff.Value.Passed)
                {
                    SetStar(challengeDiff.Key);
                }
            }
        }

        private void SetStar(Difficulty difficulty)
        {
            int imageIndex = (int) difficulty - 1;
            _starImages[imageIndex].sprite = difficulty switch
            {
                Difficulty.Easy => _easyStar,
                Difficulty.Medium => _mediumStar,
                Difficulty.Hard => _hardStar,
                Difficulty.Expert => _expertStar,
                _ => throw new Exception("wtfbbq?")
            };
        }
    }
}