using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Loading.UI.LoadingMenu
{
    [RequireComponent(typeof(Animation))]
    public class LoadingMenuFailBlock : MonoBehaviour
    {
        #region Structs

        public enum FailType
        {
            Obsolete,
            NoConnection,
            UnknownError
        }

        #endregion

        [Header("References")] 
        [SerializeField] private GameObject _background;
        [SerializeField] private LocalizeStringEvent _textLocalize;
        [SerializeField] private Button _exitButton;

        [Header("Settings")] 
        [SerializeField] private LocalizedString _obsoleteLocalizeReference;
        [SerializeField] private LocalizedString _noConnectionLocalizeReference;
        [SerializeField] private LocalizedString _unknownErrorLocalizeReference;

        private Animation _animation;
        private Animation Animation
        {
            get
            {
                if (_animation == null)
                {
                    _animation = GetComponent<Animation>();
                }

                return _animation;
            }
        }
        
        private void OnEnable()
        {
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void OnDisable()
        {
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }

        public void Show(FailType failType)
        {
            _textLocalize.StringReference = failType switch
            {
                FailType.Obsolete => _obsoleteLocalizeReference,
                FailType.NoConnection => _noConnectionLocalizeReference,
                FailType.UnknownError => _unknownErrorLocalizeReference,
                _ => _textLocalize.StringReference
            };

            _textLocalize.RefreshString();

            Animation.Play();
            
            _background.SetActive(true);
        }

        private void OnExitButtonClicked()
        {
            Application.Quit();
        }
    }
}