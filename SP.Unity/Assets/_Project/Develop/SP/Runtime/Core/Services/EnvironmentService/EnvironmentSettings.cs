using System;
using System.Linq;
using UnityEngine;

namespace SP.Runtime.Core.Services.EnvironmentService
{
    [CreateAssetMenu(menuName = "EnvironmentService/EnvironmentSettings")]
    public class EnvironmentSettings : ScriptableObject
    {
        #region Structs

        public enum TimeOfDay
        {
            Night = 0,
            Morning,
            Noon,
            Evening
        }
        
        [Serializable]
        public class TimeOfDayContainer
        {
            [Range(0, 1)] [SerializeField] private float _time;
            public float Time => _time;

            [SerializeField] private TimeOfDay _timeOfDay;
            public TimeOfDay TimeOfDay => _timeOfDay;
        }

        [Serializable]
        public class SunYPosition
        {
            [Range(0, 1)] [SerializeField] private float _time;
            public float Time => _time;

            [SerializeField] private float _y;
            public float Y => _y;
        }

        #endregion
        
        [Header("Settings")]
        [SerializeField] private TimeOfDayContainer[] _timesOfDay;
        public TimeOfDayContainer[] TimesOfDays => _timesOfDay;
        
        [Space(10)]
        
        [SerializeField] private AnimationCurve _sunXPositions;
        public AnimationCurve SunXPositions => _sunXPositions;
        
        [SerializeField] private SunYPosition[] _sunYPositions;
        public SunYPosition[] SunYPositions => _sunYPositions;
        
        [Space(10)]
        
        [SerializeField] private AnimationCurve _sunColorTemperature;
        public AnimationCurve SunColorTemperature => _sunColorTemperature;

        [SerializeField] private float _sunColorMultiplier = 1000;
        public float SunColorMultiplier => _sunColorMultiplier;
        
        [SerializeField] private AnimationCurve _sunIntensity;
        public AnimationCurve SunIntensity => _sunIntensity;

        [Space(10)] 
        
        [GradientUsage(true)] [SerializeField] private Gradient _skyColor;

        public Gradient SkyColor => _skyColor;
        
        [SerializeField] private Gradient _fogColor;
        public Gradient FogColor => _fogColor;

        [Space(10)] 
        
        [SerializeField] private AnimationCurve _dayNightBlendForPostProcessing;
        public AnimationCurve DayNightBlendForPostProcessing => _dayNightBlendForPostProcessing;
        
        public TimeOfDay GetTimeOfDay(float currentTime01)
        {
            var output = TimeOfDay.Morning; // Default value.

            currentTime01 = Mathf.Clamp01(currentTime01);

            if (_timesOfDay.Length == 0)
            {
                return output;
            }

            var timesOfDay = _timesOfDay.OrderBy(t => t.Time);

            foreach (var t in timesOfDay)
            {
                if (currentTime01 < t.Time)
                {
                    output = t.TimeOfDay;
                    break;
                }
            }

            return output;
        }
    }
}