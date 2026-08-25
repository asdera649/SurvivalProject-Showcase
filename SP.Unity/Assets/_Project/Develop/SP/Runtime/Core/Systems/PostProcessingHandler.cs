using UnityEngine;
using UnityEngine.Rendering;

namespace SP.Runtime.Core.Systems
{
    public class PostProcessingHandler : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Volume _dayGlobalVolume;
        [SerializeField] private Volume _nightGlobalVolume;

        [Header("Settings")] 
        [SerializeField, Range(0, 1)] private float _dayNightBlend = 1;
        public float DayNightBlend
        {
            get => _dayNightBlend;
            set => _dayNightBlend = Mathf.Clamp01(value);
        }

        private void Update()
        {
            _nightGlobalVolume.weight = Mathf.Lerp(1, 0, _dayNightBlend);
            _dayGlobalVolume.weight = Mathf.Lerp(0, 1, _dayNightBlend);
        }
    }
}