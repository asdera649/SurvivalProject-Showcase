using UnityEngine;

namespace SP.Runtime.Core.Camera
{
    public class ShakeHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 _maximumTranslationShake = Vector3.one * 0.5f;
        [SerializeField] private Vector3 _maximumAngularShake = Vector3.one * 2;
        [SerializeField] private float _frequency = 25;
        [SerializeField] private float _traumaExponent = 1;
        [SerializeField] private float _recoverySpeed = 1;
    
        private float _trauma;

        private float _seed;

        private void Awake()
        {
            _seed = Random.value;
        }

        private void LateUpdate()
        {
            var shake = Mathf.Pow(_trauma, _traumaExponent);
            
            transform.localPosition += new Vector3(
                _maximumTranslationShake.x * (Mathf.PerlinNoise(_seed, Time.time * _frequency) * 2 - 1),
                _maximumTranslationShake.y * (Mathf.PerlinNoise(_seed + 1, Time.time * _frequency) * 2 - 1),
                _maximumTranslationShake.z * (Mathf.PerlinNoise(_seed + 2, Time.time * _frequency) * 2 - 1)
            ) * shake;

            transform.localRotation *= Quaternion.Euler(new Vector3(
                _maximumAngularShake.x * (Mathf.PerlinNoise(_seed + 3, Time.time * _frequency) * 2 - 1),
                _maximumAngularShake.y * (Mathf.PerlinNoise(_seed + 4, Time.time * _frequency) * 2 - 1),
                _maximumAngularShake.z * (Mathf.PerlinNoise(_seed + 5, Time.time * _frequency) * 2 - 1)
            ) * shake);

            _trauma = Mathf.Clamp01(_trauma - _recoverySpeed * Time.deltaTime);
        }

        public void InduceStress(float stress)
        {
            _trauma = Mathf.Clamp01(_trauma + stress);
        }
    }
}
