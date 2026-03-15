using TMPro;
using UnityEngine;
using YARG.Menu.Navigation;
using YARG.Player;

namespace YARG.Menu.Person
{
    public class PersonView : NavigatableBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameText;

        public YargPerson Person { get; private set; }

        private PersonListMenu _personListMenu;
        private PersonSidebar _personSidebar;

        public void Init(PersonListMenu menu, YargPerson person, PersonSidebar sidebar)
        {
            _personListMenu = menu;
            _personSidebar = sidebar;
            UpdateDisplay(person);
        }

        public void UpdateDisplay(YargPerson person)
        {
            Person = person;
            _nameText.text = person.Name;
        }

        protected override void OnSelectionChanged(bool selected)
        {
            base.OnSelectionChanged(selected);

            if (selected)
            {
                _personSidebar.UpdateSidebar(Person, this);
            }
        }
    }
}