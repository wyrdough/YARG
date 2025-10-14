using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Challenge
{
    public class ChallengeView : ViewObject<ViewType>
    {
        [Space]
        [SerializeField]
        private GameObject _fullContainer;
        [SerializeField]
        private GameObject _categoryContainer;

        [Space]
        [SerializeField]
        private GameObject _scoreContainer;
        [SerializeField]
        private TextMeshProUGUI _bandScore;
        [SerializeField]
        private ChallengeStarView _starView;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void OnClick()
        {
            ViewType.ViewClick();
        }

        public override void Show(bool selected, ViewType viewType)
        {
            base.Show(selected, viewType);

            // Show the correct container
            _fullContainer.SetActive(viewType.UseFullContainer);
            _categoryContainer.SetActive(!viewType.UseFullContainer);

            var challenge = viewType.GetChallenge();
            if (challenge is not null)
            {
                _button.interactable = true;
                _starView.SetStars(challenge);
            }

            // _scoreContainer.SetActive(false);
        }

        public override void Hide()
        {
            base.Hide();

            // Use the smaller container to make the "drifts" smaller
            _fullContainer.SetActive(false);
            _categoryContainer.SetActive(true);

            _button.interactable = false;
        }
    }
}