using SP.Runtime.Meta.Services.NetworkService;
using SP.Runtime.Meta.UI.MainMenu.Blocks;
using SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public event UnityAction StartedOver;
        
        [Header("References")]
        [SerializeField] private Button _pauseMenuButton;
        
        [SerializeField] private Animation _menuAnimation;
        
        [SerializeField] private SettingsBlock _settingsBlock;
        public SettingsBlock SettingsBlock => _settingsBlock;
        
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _startOverButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;
        
        [SerializeField] private GameObject _startOverBlock;
        [SerializeField] private Button _startOverNoButton;
        [SerializeField] private Button _startOverYesButton;
        [SerializeField] private Animation _startOverBlockAnimation;
        
        [SerializeField] private GameObject _exitBlock;
        [SerializeField] private Button _exitNoButton;
        [SerializeField] private Button _exitYesButton;
        [SerializeField] private Animation _exitBlockAnimation;
        
        private const string _appearanceAnimation = "Appearance";
        private const string _disappearanceAnimation = "Disappearance";

        private void OnEnable()
        {
            _pauseMenuButton.onClick.AddListener(OnPauseMenuButtonClicked);
            _continueButton.onClick.AddListener(OnContinueButtonClicked);
            _settingsButton.onClick.AddListener(OnSettingButtonClicked);
            _startOverButton.onClick.AddListener(OnStartOverButtonClicked);
            _startOverNoButton.onClick.AddListener(OnStartOverNoButtonClicked);
            _startOverYesButton.onClick.AddListener(OnStartOverYesButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
            _exitNoButton.onClick.AddListener(OnExitNoButtonClicked);
            _exitYesButton.onClick.AddListener(OnExitYesButtonClicked);
        }

        private void OnDisable()
        {
            _pauseMenuButton.onClick.RemoveAllListeners();
            _continueButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveAllListeners();
            _startOverButton.onClick.RemoveAllListeners();
            _startOverNoButton.onClick.RemoveAllListeners();
            _startOverYesButton.onClick.RemoveAllListeners();
            _exitButton.onClick.RemoveAllListeners();
            _exitNoButton.onClick.RemoveAllListeners();
            _exitYesButton.onClick.RemoveAllListeners();
        }

        private void OnPauseMenuButtonClicked()
        {
            _menuAnimation.Play(_appearanceAnimation);
        }

        private void OnContinueButtonClicked()
        {
            _menuAnimation.Play(_disappearanceAnimation);
            
            _settingsBlock.SetView(false);
            _settingsButton.interactable = true;
            _startOverBlock.SetActive(false);
            _exitBlock.SetActive(false);
        }

        private void OnSettingButtonClicked()
        {
            _settingsBlock.SetView(true);
            _settingsButton.interactable = false;
        }

        private void OnExitButtonClicked()
        {
            _exitBlock.SetActive(true);
            _exitBlockAnimation.Play();
        }
        
        private void OnExitNoButtonClicked()
        {
            _exitBlock.SetActive(false);
        }

        private void OnExitYesButtonClicked()
        {
            NetworkService.singleton.StopHost();
        }
        
        private void OnStartOverButtonClicked()
        {
            _startOverBlock.SetActive(true);
            _startOverBlockAnimation.Play();
        }

        private void OnStartOverNoButtonClicked()
        {
            _startOverBlock.SetActive(false);
        }
        
        private void OnStartOverYesButtonClicked()
        {
            OnContinueButtonClicked();
            
            StartedOver?.Invoke();
        }
    }
}
