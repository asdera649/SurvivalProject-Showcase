using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Bootstrap.Services.SettingsService
{
    public class SettingsData
    {
        public enum LanguageSetting : uint
        {
            Russia = 0,
            English
        }
        
        public enum FramesPerSecondLimit : uint
        {
            _60 = 60,
            _30 = 30
        }
        
        public enum ResolutionQualitySetting : uint
        {
            High = 100,
            Medium = 85,
            Low = 70,
            VeryLow = 55
        }
        
        public enum TextureQualitySetting : uint
        {
            High = 0,
            Medium,
            Low,
            VeryLow
        }

        public enum MsaaQualitySetting : uint
        {
            _8x = 8,
            _4x = 4,
            _2x = 2,
            Disabled = 1
        }
        
        public enum ShadowResolutionSetting : uint
        {
            High = 0,
            Medium,
            Low,
            Disabled
        }
        
        public enum ShadersQualitySetting : uint
        {
            High = 0,
            Medium,
            Low,
        }

        public LanguageSetting Language { get; set; } = LanguageSetting.English;

        private string _nickName = NickNameUtils.GetRandomNickName();
        public string NickName
        {
            get => _nickName;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _nickName = NickNameUtils.GetRandomNickName();
                    return;
                }

                _nickName = value;
            }
        }
        
        public bool HighlightResourcesAndContainers { get; set; } = true;
        
        public bool HelpWithAiming { get; set; } = true;

        public FramesPerSecondLimit FpsLimit { get; set; } = FramesPerSecondLimit._60;
        
        public bool FrameCounterEnabled { get; set; }
        
        public ResolutionQualitySetting ResolutionQuality { get; set; } = ResolutionQualitySetting.High;

        public TextureQualitySetting TextureQuality { get; set; } = TextureQualitySetting.High;

        private float _renderScale = 1;
        public float RenderScale 
        { 
            get => _renderScale;
            set => _renderScale = Mathf.Clamp01(value);
        }

        public MsaaQualitySetting MsaaQuality = MsaaQualitySetting.Disabled;

        public ShadowResolutionSetting ShadowResolution = ShadowResolutionSetting.High;

        private float _shadowDistance = 50;
        public float ShadowDistance 
        { 
            get => _shadowDistance;
            set => _shadowDistance = Mathf.Clamp(value, _minShadowDistance, _maxShadowDistance);
        }

        public ShadersQualitySetting ShadersQuality = ShadersQualitySetting.High;

        public bool GrassEnabled { get; set; } = true;

        private float _grassSpawnDistance = 30;
        public float GrassSpawnDistance 
        { 
            get => _grassSpawnDistance;
            set => _grassSpawnDistance = Mathf.Clamp(value, _minGrassSpawnDistance, _maxGrassSpawnDistance);
        }

        private float _mainVolume = 1;
        public float MainVolume 
        { 
            get => _mainVolume;
            set => _mainVolume = Mathf.Clamp01(value);
        }

        private const float _minShadowDistance = 30, _maxShadowDistance = 100;
        public float MinShadowDistance => _minShadowDistance;
        public float MaxShadowDistance => _maxShadowDistance;
        
        private const float _minGrassSpawnDistance = 30, _maxGrassSpawnDistance = 50;
        public float MinGrassSpawnDistance => _minGrassSpawnDistance;
        public float MaxGrassSpawnDistance => _maxGrassSpawnDistance;
    }
}
