using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using YARG.Helpers.Extensions;
using YARG.Menu.Navigation;
using YARG.Player;

namespace YARG.Menu.Person
{
    public class PersonListMenu : MonoBehaviour
    {
        [SerializeField]
        private NavigationGroup _navigationGroup;

        [Space]
        [SerializeField]
        private PersonSidebar _personSidebar;
        [SerializeField]
        private Transform _personList;

        [Space]
        [SerializeField]
        private GameObject _personViewPrefab;
        [SerializeField]
        private GameObject _personListHeaderPrefab;

        public void RefreshList(YargPerson selected)
        {
            _personSidebar.HideContents();

            _personList.DestroyChildren();
            _navigationGroup.ClearNavigatables();


        }

        public void AddListGroup(string header, IEnumerable<YargPerson> people)
        {
            if (!people.Any())
            {
                return;
            }

            var headerObj = Instantiate(_personListHeaderPrefab, _personList);
            headerObj.GetComponentInChildren<TextMeshProUGUI>().text = header;
            _navigationGroup.AddNavigatable(headerObj);
        }
    }
}