using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    public class ServerListFailBlock : MonoBehaviour
    {
        #region Structs
        
        public enum FailType
        {
            Empty,
            Obsolete,
            NoConnection,
            UnknownError
        }

        #endregion

        [Header("References")] 
        [SerializeField] private GameObject _group;
        [SerializeField] private LocalizeStringEvent _textLocalize;
        
        [Header("Settings")] 
        [SerializeField] private LocalizedString _emptyLocalizeReference;
        [SerializeField] private LocalizedString _obsoleteLocalizeReference;
        [SerializeField] private LocalizedString _noConnectionLocalizeReference;
        [SerializeField] private LocalizedString _unknownErrorLocalizeReference;
        
        public void Show(FailType failType)
        {
            _textLocalize.StringReference = failType switch
            {
                FailType.Empty => _emptyLocalizeReference,
                FailType.Obsolete => _obsoleteLocalizeReference,
                FailType.NoConnection => _noConnectionLocalizeReference,
                FailType.UnknownError => _unknownErrorLocalizeReference,
                _ => _textLocalize.StringReference
            };

            _textLocalize.RefreshString();
            
            _group.SetActive(true);
        }

        public void Hide()
        {
            _group.SetActive(false);
        }
    }
}