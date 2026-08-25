using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SP.Runtime.Localization
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LocalizeDropdown : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<LocalizedString> _dropdownOptions;

        private TMP_Dropdown _tmpDropdown;
        private TMP_Dropdown TmpDropdown
        {
            get
            {
                if (_tmpDropdown == null)
                {
                    _tmpDropdown = GetComponent<TMP_Dropdown>();
                }

                return _tmpDropdown;
            }
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            
            OnLocaleChanged(LocalizationSettings.SelectedLocale);
        }
        
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale value)
        {
            List<TMP_Dropdown.OptionData> tmpDropdownOptions = new();

            foreach (var o in _dropdownOptions)
            {
                tmpDropdownOptions.Add(new TMP_Dropdown.OptionData(o.GetLocalizedString()));
            }

            TmpDropdown.options = tmpDropdownOptions;
        }
    }

    public abstract class AddLocalizeDropdown
    {
#if UNITY_EDITOR
        [MenuItem("CONTEXT/TMP_Dropdown/Localize", false, 1)]
        private static void AddLocalizeComponent()
        {
            var selected = Selection.activeGameObject;

            if (selected != null)
            {
                selected.AddComponent<LocalizeDropdown>();
            }
        }
#endif
    }
}