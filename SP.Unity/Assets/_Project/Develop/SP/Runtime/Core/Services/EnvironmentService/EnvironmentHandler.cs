using SP.Runtime.Core.Systems;
using UnityEngine;

namespace SP.Runtime.Core.Services.EnvironmentService
{
    public class EnvironmentHandler
    {
        public EnvironmentHandler(EnvironmentSettings settings)
        {
            _environmentSettings = settings;
        }

        private float _time;
        private float Time
        {
            get => _time;
            set => _time = Mathf.Clamp01(value);
        }
        
        private readonly EnvironmentSettings _environmentSettings;

        public void Apply(
            float time,
            ref DirectionalLight directionalLight,
            ref PostProcessingHandler postProcessingHandler)
        {
            Time = time;
            
            UpdatePosition(directionalLight.transform);
            UpdateColors(directionalLight.Light);
            UpdatePostProcessing(postProcessingHandler);
        }
        
        private void UpdatePosition(Transform transform)
        {
            for (var i = 0; i < _environmentSettings.SunYPositions.Length; i++)
            {
                var end = i != _environmentSettings.SunYPositions.Length - 1 ? i + 1 : 0;
            
                if (_environmentSettings.SunYPositions[i].Time <= Time &&
                    Time <= _environmentSettings.SunYPositions[end].Time)
                {
                    var percent = Mathf.Abs(_environmentSettings.SunYPositions[end].Time -
                                             _environmentSettings.SunYPositions[i].Time) / 100;
                    
                    var current = Mathf.Abs(Time - _environmentSettings.SunYPositions[i].Time);

                    var x = Mathf.Clamp01(_environmentSettings.SunXPositions.Evaluate(Time)) * 90;
                    
                    var oldEuler = new Vector3(x, _environmentSettings.SunYPositions[i].Y, 0);
                    var newEuler = new Vector3(x, _environmentSettings.SunYPositions[end].Y, 0);
                    
                    transform.localRotation = Quaternion.Slerp(
                        Quaternion.Euler(oldEuler),
                        Quaternion.Euler(newEuler),
                        current / percent * 0.01f);
            
                    break;
                }
            
            }
        }

        private void UpdateColors(Light light)
        {
            light.colorTemperature = _environmentSettings.SunColorTemperature.Evaluate(Time) * 
                                     _environmentSettings.SunColorMultiplier;
            
            light.intensity = _environmentSettings.SunIntensity.Evaluate(Time);
            RenderSettings.ambientLight = _environmentSettings.SkyColor.Evaluate(Time);
            RenderSettings.fogColor = _environmentSettings.FogColor.Evaluate(Time);
        }

        private void UpdatePostProcessing(PostProcessingHandler postProcessingHandler)
        {
            postProcessingHandler.DayNightBlend = _environmentSettings.DayNightBlendForPostProcessing.Evaluate(Time);
        }
    }
}