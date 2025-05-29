using TMPro;
using UnityEngine;
using YARG.Core.Engine.Drums;

namespace YARG.Menu.ScoreScreen
{
    public class DrumsScoreCard : ScoreCard<DrumsStats>
    {
        [Space]
        [SerializeField]
        private TextMeshProUGUI _overhits;

        public override void SetCardContents(bool singleplayer = true)
        {
            base.SetCardContents(singleplayer);

            _overhits.text = WrapWithColor(Stats.Overhits);
        }
    }
}