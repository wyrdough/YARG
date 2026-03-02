using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private Button _addProfileButton;
        private Button _addLeaderboardButton;
    }
}