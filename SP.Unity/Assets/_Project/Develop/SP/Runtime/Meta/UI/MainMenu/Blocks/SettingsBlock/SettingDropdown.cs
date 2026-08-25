using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock
{
    public class SettingDropdown : TMP_Dropdown
    {
        public UnityEvent OnDropdownOpened;
        public UnityEvent OnDropdownClosed;
        
        protected override GameObject CreateDropdownList(GameObject dropdownTemplate)
        {
            var output = base.CreateDropdownList(dropdownTemplate);
            
            OnDropdownOpened?.Invoke();

            return output;
        }
        
        protected override void DestroyDropdownList(GameObject dropdownList)
        {
            base.DestroyDropdownList(dropdownList);
            
            OnDropdownClosed?.Invoke();
        }
    }
}