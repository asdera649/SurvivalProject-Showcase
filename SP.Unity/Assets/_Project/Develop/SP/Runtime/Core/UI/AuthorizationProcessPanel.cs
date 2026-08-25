using SP.Runtime.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI
{
    [RequireComponent(typeof(Animation))]
    public class AuthorizationProcessPanel : MonoBehaviour
    {
        public event UnityAction Exit;
        
        [Header("References")] 
        [SerializeField] private Image _loadingIcon;
        [SerializeField] private LocalizeStringEvent _titleLocalize;
        [SerializeField] private Button _exitButton;
        [SerializeField] private LocalizeStringHelper _exitLocalize;

        [Header("Settings")] 
        [SerializeField] private float _failedAuthorizationTimeout = 30;
        [SerializeField] private float _enablingExitButtonTimeout = 5;
        [SerializeField] private float _loadingIconRotateSpeed = 500;
        
        private const float _oneSecond = 1;

        private float _timeToFailedAuthorization;
        private float _timeToEnablingExitButton;

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

        private void Start()
        {
            _exitButton.interactable = false;
            _exitButton.onClick.AddListener(OnExitButtonClicked);

            _timeToFailedAuthorization = _failedAuthorizationTimeout;
            _timeToEnablingExitButton = _enablingExitButtonTimeout;
            
            InvokeRepeating(nameof(OnUpdate), _oneSecond, _oneSecond);
        }

        private void OnDestroy()
        {
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
            
            CancelInvoke(nameof(OnUpdate));
        }

        private void Update()
        {
            UpdateLoadingIcon();
        }

        private void UpdateLoadingIcon()
        {
            _loadingIcon.rectTransform.Rotate(0, 0, _loadingIconRotateSpeed * Time.deltaTime);
        }

        private void OnUpdate()
        {
            _exitButton.interactable = _timeToEnablingExitButton <= 0;
            _exitLocalize.SetEntry(_timeToEnablingExitButton <= 0 ? "{Exit}" : "{Exit} (" + _timeToEnablingExitButton + ")");

            if (_timeToFailedAuthorization <= 0)
            {
                _titleLocalize.SetEntry("AuthorizationFailed");
                
                CancelInvoke(nameof(OnUpdate));
            }
            
            _timeToFailedAuthorization -= _oneSecond;
            _timeToEnablingExitButton -= _oneSecond;
        }

        public void Destroy()
        {
            Animation.Play();
        }

        #region AnimationEvents

        public void DisappearingEnd()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Callbacks

        private void OnExitButtonClicked()
        {
            Exit?.Invoke();
        }
        
        #endregion
    }
}