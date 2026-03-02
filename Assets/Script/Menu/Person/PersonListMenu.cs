using UnityEngine;
using YARG.Menu.Navigation;

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


    }
}