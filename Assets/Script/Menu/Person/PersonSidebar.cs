using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Player;

namespace YARG.Menu.Person
{
    public class PersonSidebar : MonoBehaviour
    {
        [SerializeField]
        private GameObject _contents;
        [SerializeField]
        private TextMeshProUGUI _name;
        [SerializeField]
        private TMP_InputField _nameInput;
        [Space]
        [SerializeField]
        private GameObject _profileList;
        [SerializeField]
        private GameObject _profilePrefab;
        [Space]
        [SerializeField]
        private GameObject _leaderboardList;
        [SerializeField]
        private GameObject _leaderboardPrefab;

        [Space]
        [SerializeField]
        private GameObject _categoryHeaderPrefab;

        [Space]
        [SerializeField]
        private Button _addProfileButton;
        private Button _addLeaderboardButton;

        private PersonView _personView;

        public void HideContents()
        {
            _contents.SetActive(false);
        }

        public void ShowContents()
        {
            _contents.SetActive(true);
        }

        public void UpdateSidebar(YargPerson person, PersonView view)
        {
            _name.text = person.Name;
            _nameInput.text = person.Name;
            _personView = view;

            HideContents();
            DestroyChildren();
            Populate();
            ShowContents();
        }

        private void Populate()
        {
            // First profiles, then leaderboards
            var profileHeader = Instantiate(_categoryHeaderPrefab, _contents.transform);
            profileHeader.GetComponentInChildren<TextMeshProUGUI>().text = "Profiles";

            foreach (var foo in _personView.Person.Profiles)
            {
                var profile = Instantiate(_profilePrefab, _contents.transform);
                // Somehow init the profile "list"
            }

            var leaderboardHeader = Instantiate(_categoryHeaderPrefab, _contents.transform);
            leaderboardHeader.GetComponentInChildren<TextMeshProUGUI>().text = "Leaderboards";

            foreach (var foo in _personView.Person.Leaderboards)
            {
                var leaderboard = Instantiate(_leaderboardPrefab, _contents.transform);
                // Somehow init the leaderboard "list"
            }
        }

        private void DestroyChildren()
        {
            foreach (var child in _contents.GetComponentsInChildren<GameObject>())
            {
                if (child == null)
                {
                    continue;
                }

                Destroy(child);
            }
        }
    }
}