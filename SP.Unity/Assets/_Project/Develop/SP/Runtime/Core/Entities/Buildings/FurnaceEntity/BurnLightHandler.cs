using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings.FurnaceEntity
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BurnLightHandler : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Color _minIntensityColor;
        [SerializeField] private Color _maxIntensityColor;
        [SerializeField] private float _flickerSpeed = 0.05f;
        [SerializeField] private float _flickerSpeedFloatingCoefficient = 0.11f;
        [SerializeField] private float _transitionSpeed = 1;

        private Color _targetColor;
        private Color _currentColor = DisabledColor;
        
        private bool _isActive;

        private float _percent;
        private float Percent
        {
            get => _percent;
            set => _percent = Mathf.Clamp01(value);
        }

        private bool _isIncreasing = true;

        private static readonly Color DisabledColor = Color.black;
        
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        private MeshRenderer _meshRenderer;
        private MeshRenderer MeshRenderer
        {
            get
            {
                if (_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }

                return _meshRenderer;
            }
        }
        
        private void Update()
        {
            if (_isActive)
            {
                UpdatePercent();
                
                _targetColor = Color.Lerp(_minIntensityColor, _maxIntensityColor, Percent);

                _currentColor = _targetColor;
            }
            else
            {
                _currentColor = Color.Lerp(_currentColor, DisabledColor, Time.deltaTime * _transitionSpeed);
            }

            if (_currentColor == DisabledColor)
            {
                return;
            }
            
            MeshRenderer.material.SetColor(EmissionColor, _currentColor);
        }

        private void UpdatePercent()
        {
            var speed = _flickerSpeed + Random.Range(0, _flickerSpeedFloatingCoefficient);
            
            if (_isIncreasing)
            {
                Percent += speed;
            }
            else
            {
                Percent -= speed;
            }

            if (Percent == 1)
            {
                _isIncreasing = false;
            }
            else if (Percent == 0)
            {
                _isIncreasing = true;
            }
        }

        public void SetActive(bool value)
        {
            _isActive = value;
        }
    }
}