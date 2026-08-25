using CI.QuickSave;
using CI.QuickSave.Core.Storage;
using Cysharp.Threading.Tasks;
using FMODUnity;
using SP.Runtime.Core.Systems.GrassGeneration;
using SP.Runtime.Core.Systems.MiningDetection;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.LoadingService;
using SP.Runtime.Utilities.FpsDisplay;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SP.Runtime.Bootstrap.Services.SettingsService
{
    public class SettingsService : MonoBehaviour, ILoadUnit
    {
        [Header("References"), Header("Localization")]
        [SerializeField] private LocalizationSettings _localizationSettings;
        [SerializeField] private Locale[] _locales;

        [Header("Materials and Shaders")]
        [SerializeField] private Material _birchLeavesMaterial;
        [SerializeField] private Material _bushLeavesMaterial;
        [SerializeField] private Material _firBranchMaterial;
        [SerializeField] private Material[] _grassMaterials;

        [SerializeField] private Shader _vegetationShader;
        [SerializeField] private Shader _simpleVegetationShader;
        [SerializeField] private Shader _grassShader;
        [SerializeField] private Shader _simpleGrassShader;

        [Header("ScriptableObjects")] 
        [SerializeField] private FpsDisplaySettings _fpsDisplaySettings;
        [SerializeField] private GrassGeneratorSettings _grassGeneratorSettings;
        [SerializeField] private MiningDetectorSettings _miningDetectorSettings;
        [SerializeField] private HudSettings _hudSettings;
        
        public SettingsData SettingsData { get; private set; }
    
        private const string _fileName = "Settings";

        private bool _isInitialized;
        
        public UniTask Load()
        {
            if (_isInitialized)
            {
                return UniTask.CompletedTask;
            }

            SettingsData = new SettingsData();
        
            if (FileAccess.Exists(_fileName, false))
            {
                LoadSave();
            }
            else
            {
                Save();
            }
            
            ApplySettings();

            _isInitialized = true;
            
            return UniTask.CompletedTask;
        }
        
        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Save()
        {
            if (_isInitialized)
            {
                ApplySettings();
            }

            var writer = QuickSaveWriter.Create(_fileName);

            writer.Write(nameof(SettingsData.Language), SettingsData.Language);
            writer.Write(nameof(SettingsData.NickName), SettingsData.NickName);
            writer.Write(nameof(SettingsData.HighlightResourcesAndContainers), SettingsData.HighlightResourcesAndContainers);
            writer.Write(nameof(SettingsData.HelpWithAiming), SettingsData.HelpWithAiming);
            writer.Write(nameof(SettingsData.FpsLimit), SettingsData.FpsLimit);
            writer.Write(nameof(SettingsData.FrameCounterEnabled), SettingsData.FrameCounterEnabled);
            writer.Write(nameof(SettingsData.ResolutionQuality), SettingsData.ResolutionQuality);
            writer.Write(nameof(SettingsData.TextureQuality), SettingsData.TextureQuality);
            writer.Write(nameof(SettingsData.RenderScale), SettingsData.RenderScale);
            writer.Write(nameof(SettingsData.MsaaQuality), SettingsData.MsaaQuality);
            writer.Write(nameof(SettingsData.ShadowResolution), SettingsData.ShadowResolution);
            writer.Write(nameof(SettingsData.ShadowDistance), SettingsData.ShadowDistance);
            writer.Write(nameof(SettingsData.ShadersQuality), SettingsData.ShadersQuality);
            writer.Write(nameof(SettingsData.GrassEnabled), SettingsData.GrassEnabled);
            writer.Write(nameof(SettingsData.GrassSpawnDistance), SettingsData.GrassSpawnDistance);
            writer.Write(nameof(SettingsData.MainVolume), SettingsData.MainVolume);
            
            writer.Commit();
        }

        private void LoadSave()
        {
            var reader = QuickSaveReader.Create(_fileName);

            if (reader.TryRead<SettingsData.LanguageSetting>(nameof(SettingsData.Language), out var language))
            {
                SettingsData.Language = language;
            }

            if (reader.TryRead<string>(nameof(SettingsData.NickName), out var nickName))
            {
                SettingsData.NickName = nickName;
            }
                
            if (reader.TryRead<bool>(nameof(SettingsData.HighlightResourcesAndContainers), out var highlightResourcesAndContainers))
            {
                SettingsData.HighlightResourcesAndContainers = highlightResourcesAndContainers;
            }
            
            if (reader.TryRead<bool>(nameof(SettingsData.HelpWithAiming), out var helpWithAiming))
            {
                SettingsData.HelpWithAiming = helpWithAiming;
            }

            if (reader.TryRead<SettingsData.FramesPerSecondLimit>(nameof(SettingsData.FpsLimit), out var fpsLimit))
            {
                SettingsData.FpsLimit = fpsLimit;
            }

            if (reader.TryRead<bool>(nameof(SettingsData.FrameCounterEnabled), out var frameCounterEnabled))
            {
                SettingsData.FrameCounterEnabled = frameCounterEnabled;
            }

            if (reader.TryRead<SettingsData.ResolutionQualitySetting>(nameof(SettingsData.ResolutionQuality),
                    out var resolutionQuality))
            {
                SettingsData.ResolutionQuality = resolutionQuality;
            }

            if (reader.TryRead<SettingsData.TextureQualitySetting>(nameof(SettingsData.TextureQuality),
                    out var textureQuality))
            {
                SettingsData.TextureQuality = textureQuality;
            }

            if (reader.TryRead<float>(nameof(SettingsData.RenderScale), out var renderScale))
            {
                SettingsData.RenderScale = renderScale;
            }

            if (reader.TryRead<SettingsData.MsaaQualitySetting>(nameof(SettingsData.MsaaQuality), out var msaaQuality))
            {
                SettingsData.MsaaQuality = msaaQuality;
            }

            if (reader.TryRead<SettingsData.ShadowResolutionSetting>(nameof(SettingsData.ShadowResolution),
                    out var shadowResolution))
            {
                SettingsData.ShadowResolution = shadowResolution;
            }

            if (reader.TryRead<float>(nameof(SettingsData.ShadowDistance), out var shadowDistance))
            {
                SettingsData.ShadowDistance = shadowDistance;
            }

            if (reader.TryRead<SettingsData.ShadersQualitySetting>(nameof(SettingsData.ShadersQuality),
                    out var shadersQuality))
            {
                SettingsData.ShadersQuality = shadersQuality;
            }

            if (reader.TryRead<bool>(nameof(SettingsData.GrassEnabled), out var grassEnabled))
            {
                SettingsData.GrassEnabled = grassEnabled;
            }

            if (reader.TryRead<float>(nameof(SettingsData.GrassSpawnDistance), out var grassSpawnDistance))
            {
                SettingsData.GrassSpawnDistance = grassSpawnDistance;
            }

            if (reader.TryRead<float>(nameof(SettingsData.MainVolume), out var mainVolume))
            {
                SettingsData.MainVolume = mainVolume;
            }
        }

        private void ApplySettings()
        {
            _localizationSettings.SetSelectedLocale(_locales[(int)SettingsData.Language]);

            QualitySettings.SetQualityLevel((int)SettingsData.ShadowResolution);
            Application.targetFrameRate = (int)SettingsData.FpsLimit;
            QualitySettings.resolutionScalingFixedDPIFactor = (float)SettingsData.ResolutionQuality / 100;
            QualitySettings.globalTextureMipmapLimit = (int)SettingsData.TextureQuality;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset data)
            {
                data.renderScale = SettingsData.RenderScale;
                data.msaaSampleCount = (int)SettingsData.MsaaQuality;
                data.shadowDistance = SettingsData.ShadowDistance;
            }

            switch ((int)SettingsData.ShadersQuality)
            {
                case 0:
                {
                    _birchLeavesMaterial.shader = _vegetationShader;
                    _bushLeavesMaterial.shader = _vegetationShader;
                    _firBranchMaterial.shader = _vegetationShader;
                    
                    foreach (var m in _grassMaterials)
                    {
                        m.shader = _grassShader;
                    }
                    
                    break;
                }
                case 1:
                {
                    _birchLeavesMaterial.shader = _vegetationShader;
                    _bushLeavesMaterial.shader = _vegetationShader;
                    _firBranchMaterial.shader = _vegetationShader;
                    
                    foreach (var m in _grassMaterials)
                    {
                        m.shader = _simpleGrassShader;
                    }
                    
                    break;
                }
                case 2:
                {
                    _birchLeavesMaterial.shader = _simpleVegetationShader;
                    _bushLeavesMaterial.shader = _simpleVegetationShader;
                    _firBranchMaterial.shader = _simpleVegetationShader;
                    
                    foreach (var m in _grassMaterials)
                    {
                        m.shader = _simpleGrassShader;
                    }
                    
                    break;
                }
            }
            
            _fpsDisplaySettings.ShowFps = SettingsData.FrameCounterEnabled;
            
            _grassGeneratorSettings.SpawnRadius = SettingsData.GrassEnabled ? SettingsData.GrassSpawnDistance : 0;
            
            _miningDetectorSettings.IsEnabled = SettingsData.HighlightResourcesAndContainers;
            
            _hudSettings.HelpWithAiming = SettingsData.HelpWithAiming;
            
            RuntimeManager.GetBus("bus:/").setVolume(SettingsData.MainVolume);
            
            AudioListener.volume = SettingsData.MainVolume;
        }

        #region Callbacks
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            ApplySettings();
        }
        
        #endregion
    }
}
