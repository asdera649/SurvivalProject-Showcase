using SP.Runtime.Bootstrap.Services.SettingsService;
using SP.Runtime.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock
{
    public class SettingsBlock : BaseBlock
    {
        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;

        [SerializeField] private Button _gameSettingsTab;
        [SerializeField] private Button _videoSettingsTab;
        [SerializeField] private Button _audioSettingsTab;

        [SerializeField] private GameObject _gameSettingsContainer;
        [SerializeField] private GameObject _videoSettingsContainer;
        [SerializeField] private GameObject _audioSettingsContainer;

        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private TMP_Text _saveStatusText;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown _languageDropdown;
        //[SerializeField] private Toggle _shadowBreakingToggle;
        [SerializeField] private TMP_InputField _nickNameInputField;
        [SerializeField] private Toggle _highlightResourcesAndContainersToggle;
        [SerializeField] private Toggle _helpWithAimingToggle;

        [Header("Video")]
        [SerializeField] private TMP_Dropdown _fpsLimitDropdown;
        [SerializeField] private Toggle _frameCounterToggle;
        [SerializeField] private TMP_Dropdown _resolutionQualityDropdown;
        [SerializeField] private TMP_Dropdown _textureQualityDropdown;
        [SerializeField] private Slider _renderScaleSlider;
        //[SerializeField] private TMP_Dropdown _msaaQualityDropdown;
        [SerializeField] private TMP_Dropdown _shadowResolutionDropdown;
        [SerializeField] private Slider _shadowDistanceSlider;
        //[SerializeField] private TMP_Dropdown _shadersQualityDropdown;
        [SerializeField] private Toggle _grassEnabledToggle;
        [SerializeField] private Slider _grassSpawnDistanceSlider;

        [Header("Audio")]
        [SerializeField] private Slider _mainVolumeSlider;

        private GameObject _activeTab;
        private GameObject ActiveTab
        {
            set
            {
                var oldValue = _activeTab;
                _activeTab = value;
                
                OnActiveTabUpdate(oldValue, _activeTab);
            }
        }

        private bool _isUnsaved;
        private bool IsUnsaved
        {
            set
            {
                var oldValue = _isUnsaved;
                _isUnsaved = value;
                
                OnUnsavedUpdate(oldValue, _isUnsaved);
            }
        }

        private const float _activeTabButtonTransparency = 0.05f;
        private const float _inactiveTabButtonTransparency = 0.01f;

        private SettingsService _settingsService;
        
        [Inject]
        private void Inject(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }
        
        // Пока сделал так, но в будущем хотелось бы переделать,
        // так что бы, элементы настроек (Dropdown, Toggle, Slider и тд.) настраивались динамический
        // в зависимости от Enum'ов, констант и других конфигов
        private void OnEnable()
        {
            _gameSettingsTab.onClick.AddListener(OnGameSettingsTabClick);
            _videoSettingsTab.onClick.AddListener(OnVideoSettingsTabClick);
            _audioSettingsTab.onClick.AddListener(OnAudioSettingsTabClick);
            
            _languageDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            //_shadowBreakingToggle.onValueChanged.AddListener(OnToggleUpdate);
            _nickNameInputField.onValueChanged.AddListener(OnInputFieldUpdate);
            _highlightResourcesAndContainersToggle.onValueChanged.AddListener(OnToggleUpdate);
            _helpWithAimingToggle.onValueChanged.AddListener(OnToggleUpdate);

            _fpsLimitDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _frameCounterToggle.onValueChanged.AddListener(OnToggleUpdate);
            _resolutionQualityDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _textureQualityDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _renderScaleSlider.onValueChanged.AddListener(OnSliderUpdate);
            //_msaaQualityDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _shadowResolutionDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _shadowDistanceSlider.onValueChanged.AddListener(OnSliderUpdate);
            //_shadersQualityDropdown.onValueChanged.AddListener(OnDropdownUpdate);
            _grassEnabledToggle.onValueChanged.AddListener(OnToggleUpdate);
            _grassSpawnDistanceSlider.onValueChanged.AddListener(OnSliderUpdate);

            _mainVolumeSlider.onValueChanged.AddListener(OnSliderUpdate);

            _saveButton.onClick.AddListener(OnSaveButtonClick);
            _resetButton.onClick.AddListener(OnResetButtonClick);
        }

        private void Start()
        {
            ActiveTab = _gameSettingsContainer;
            
            IsUnsaved = false;
            
            UpdateSettingsBlock();
        }
        
        private void OnDisable()
        {
            _gameSettingsTab.onClick.RemoveAllListeners();
            _videoSettingsTab.onClick.RemoveAllListeners();
            _audioSettingsTab.onClick.RemoveAllListeners();

            _languageDropdown.onValueChanged.RemoveAllListeners();
            //_shadowBreakingToggle.onValueChanged.RemoveAllListeners();
            _nickNameInputField.onValueChanged.RemoveAllListeners();
            _highlightResourcesAndContainersToggle.onValueChanged.RemoveAllListeners();
            _helpWithAimingToggle.onValueChanged.RemoveAllListeners();

            _fpsLimitDropdown.onValueChanged.RemoveAllListeners();
            _frameCounterToggle.onValueChanged.RemoveAllListeners();
            _resolutionQualityDropdown.onValueChanged.RemoveAllListeners();
            _textureQualityDropdown.onValueChanged.RemoveAllListeners();
            _renderScaleSlider.onValueChanged.RemoveAllListeners();
            //_msaaQualityDropdown.onValueChanged.RemoveAllListeners();
            _shadowResolutionDropdown.onValueChanged.RemoveAllListeners();
            _shadowDistanceSlider.onValueChanged.RemoveAllListeners();
            //_shadersQualityDropdown.onValueChanged.RemoveAllListeners();
            _grassEnabledToggle.onValueChanged.RemoveAllListeners();
            _grassSpawnDistanceSlider.onValueChanged.RemoveAllListeners();

            _mainVolumeSlider.onValueChanged.RemoveAllListeners();

            _saveButton.onClick.RemoveAllListeners();
            _resetButton.onClick.RemoveAllListeners();
        }

        private void UpdateSettingsBlock()
        {
            var settingsData = _settingsService.SettingsData;
            
            _languageDropdown.value = EnumUtils.GetValueIndex(settingsData.Language);
            //_shadowBreakingToggle.isOn = settingsData.ShadowBreakingEnabled;
            _highlightResourcesAndContainersToggle.isOn = settingsData.HighlightResourcesAndContainers;
            _helpWithAimingToggle.isOn = settingsData.HelpWithAiming;
            _nickNameInputField.text = settingsData.NickName;
            _fpsLimitDropdown.value = EnumUtils.GetValueIndex(settingsData.FpsLimit);
            _frameCounterToggle.isOn = settingsData.FrameCounterEnabled;
            _resolutionQualityDropdown.value = EnumUtils.GetValueIndex(settingsData.ResolutionQuality);
            _textureQualityDropdown.value = EnumUtils.GetValueIndex(settingsData.TextureQuality);
            _renderScaleSlider.value = settingsData.RenderScale;
            //_msaaQualityDropdown.value = EnumUtils.GetValueIndex(settingsData.MsaaQuality);
            _shadowResolutionDropdown.value = EnumUtils.GetValueIndex(settingsData.ShadowResolution);
            _shadowDistanceSlider.value = settingsData.ShadowDistance;
            //_shadersQualityDropdown.value = EnumUtils.GetValueIndex(settingsData.ShadersQuality);
            _grassEnabledToggle.isOn = settingsData.GrassEnabled;
            _grassSpawnDistanceSlider.value = settingsData.GrassSpawnDistance;
            
            _mainVolumeSlider.value = settingsData.MainVolume;
        }
        
        private void OnSaveButtonClick()
        {
            var settingsData = _settingsService.SettingsData;

            settingsData.Language = EnumUtils.GetValueByIndex<SettingsData.LanguageSetting>(_languageDropdown.value);
            //settingsData.ShadowBreakingEnabled = _shadowBreakingToggle.isOn;
            settingsData.NickName = _nickNameInputField.text;
            settingsData.HighlightResourcesAndContainers = _highlightResourcesAndContainersToggle.isOn;
            settingsData.HelpWithAiming = _helpWithAimingToggle.isOn;
            
            settingsData.FpsLimit = EnumUtils.GetValueByIndex<SettingsData.FramesPerSecondLimit>(_fpsLimitDropdown.value);
            settingsData.FrameCounterEnabled = _frameCounterToggle.isOn;
            
            settingsData.ResolutionQuality = EnumUtils.GetValueByIndex<SettingsData.ResolutionQualitySetting>(
                _resolutionQualityDropdown.value);
            
            settingsData.TextureQuality = EnumUtils.GetValueByIndex<SettingsData.TextureQualitySetting>(
                _textureQualityDropdown.value);
            
            settingsData.RenderScale = _renderScaleSlider.value;
            //settingsData.MsaaQuality = EnumUtils.GetValueByIndex<SettingsData.MsaaQualitySetting>(
            //    _msaaQualityDropdown.value);
            
            settingsData.ShadowResolution = EnumUtils.GetValueByIndex<SettingsData.ShadowResolutionSetting>(
                _shadowResolutionDropdown.value);
            
            settingsData.ShadowDistance = _shadowDistanceSlider.value;
            //settingsData.ShadersQuality = EnumUtils.GetValueByIndex<SettingsData.ShadersQualitySetting>(
            //    _shadersQualityDropdown.value);
            
            settingsData.GrassEnabled = _grassEnabledToggle.isOn;
            settingsData.GrassSpawnDistance = _grassSpawnDistanceSlider.value;
            settingsData.MainVolume = _mainVolumeSlider.value;

            _settingsService.Save();
            
            UpdateSettingsBlock();
            
            IsUnsaved = false;
            
            _saveStatusText.gameObject.SetActive(true);
        }

        private void OnResetButtonClick()
        {
            UpdateSettingsBlock();
            
            IsUnsaved = false;
        }
        
        private void OnGameSettingsTabClick()
        {
            ActiveTab = _gameSettingsContainer;
        }

        private void OnVideoSettingsTabClick()
        {
            ActiveTab = _videoSettingsContainer;
        }

        private void OnAudioSettingsTabClick()
        {
            ActiveTab = _audioSettingsContainer;
        }
        
        private void OnDropdownUpdate(int value)
        {
            IsUnsaved = true;
        }
        
        private void OnInputFieldUpdate(string value)
        {
            IsUnsaved = true;
        }

        private void OnToggleUpdate(bool value)
        {
            IsUnsaved = true;
        }

        private void OnSliderUpdate(float value)
        {
            IsUnsaved = true;
        }
        
        protected override void OnViewUpdate(bool value)
        {
            OnResetButtonClick();
        }

        private void OnActiveTabUpdate(GameObject oldTab, GameObject newTab)
        {
            if (oldTab != null)
            {
                oldTab.SetActive(false);

                if (oldTab == _gameSettingsContainer)
                {
                    _gameSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _gameSettingsTab.image.color,
                        _inactiveTabButtonTransparency);
                }
                else if (oldTab == _videoSettingsContainer)
                {
                    _videoSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _videoSettingsTab.image.color,
                        _inactiveTabButtonTransparency);
                }
                else if (oldTab == _audioSettingsContainer)
                {
                    _audioSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _audioSettingsTab.image.color,
                        _inactiveTabButtonTransparency);
                }
            }

            if (newTab != null)
            {
                _scrollRect.content = newTab.GetComponent<RectTransform>();
                
                newTab.SetActive(true);

                if (newTab == _gameSettingsContainer)
                {
                    _gameSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _gameSettingsTab.image.color,
                        _activeTabButtonTransparency);
                }
                else if (newTab == _videoSettingsContainer)
                {
                    _videoSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _videoSettingsTab.image.color,
                        _activeTabButtonTransparency);
                }
                else if (newTab == _audioSettingsContainer)
                {
                    _audioSettingsTab.image.color = ColorUtils.ChangeColorAlpha(
                        _audioSettingsTab.image.color,
                        _activeTabButtonTransparency);
                }
            }

            _scrollRect.verticalNormalizedPosition = 1;
            _scrollRect.StopMovement();
        }

        private void OnUnsavedUpdate(bool oldValue, bool newValue)
        {
            _saveButton.interactable = newValue;
            _resetButton.interactable = newValue;

            if (newValue)
            {
                _saveStatusText.gameObject.SetActive(false);
            }
        }
    }
}
