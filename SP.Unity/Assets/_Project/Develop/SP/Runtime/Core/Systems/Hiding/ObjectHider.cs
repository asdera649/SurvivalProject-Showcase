using System.Collections.Generic;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Systems.Hiding
{
    public class ObjectHider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer[] _meshRenderers;
        
        [FormerlySerializedAs("_hideMeshRenderers")] 
        [SerializeField] private MeshRenderer[] _hiddenMeshRenderers;
        [SerializeField] private Collider[] _colliders;
        
        private float _currentTransparency = 1;
        private float CurrentTransparency
        {
            get => _currentTransparency;
            set => _currentTransparency = Mathf.Clamp01(value);
        }

        private const float _fadeTime = 7f;
        
        private readonly int _colorProperty = Shader.PropertyToID("_Color");

        private float _hideCooldown;
        
        #region Cache
        
        private int[] _layerCache;
        private readonly List<Material> _materialsCache = new();
        
        #endregion
        
        private void Awake()
        {
            Cache();
        }

        private void Cache()
        {
            _layerCache = new int[_colliders.Length];

            for (var i = 0; i < _colliders.Length; i++)
            {
                _layerCache[i] = _colliders[i].gameObject.layer;
            }
            
            foreach (var m in _meshRenderers)
            {
                _materialsCache.AddRange(m.materials);
            }
        }

        private void OnDestroy()
        {
            ResetObject();
        }

        private void Update()
        {
            UpdateMaterials();
            
            _hideCooldown -= Time.deltaTime;
        }

        private void UpdateMaterials()
        {
            SetMaterialsTransparency(CurrentTransparency);
        }

        private void SetMaterialsTransparency(float value01, bool lerp = true)
        {
            foreach (var m in _materialsCache)
            {
                if (m.HasColor(_colorProperty))
                {
                    var oldColor = m.GetColor(_colorProperty);
                    
                    m.SetColor(
                        _colorProperty,
                        lerp ? 
                            Color.Lerp(oldColor, ColorUtils.ChangeColorAlpha(oldColor, CurrentTransparency), _fadeTime * Time.deltaTime) : 
                            ColorUtils.ChangeColorAlpha(oldColor, CurrentTransparency));
                }
            }
        }

        public void ReduceTransparency(float value01)
        {
            SetIgnoreRaycastLayer(true);

            CurrentTransparency = value01;
            
            // Это для того что бы если после отключения скрытия объекта, мы этот объект сразу же делаем прозрачным,
            // не было 'мигание' объекта.
            if (_hideCooldown > 0)
            {
                SetMaterialsTransparency(CurrentTransparency, false);
            }
        }
        
        public void ResetTransparency()
        {
            SetIgnoreRaycastLayer(false);

            CurrentTransparency = 1;
        }

        public void Hide(bool value)
        {
            SetIgnoreRaycastLayer(value);

            // todo: возможно лучше сделать MeshRenderer.castShadows=shadowOnly, для того что бы 
            // todo: когда включали скрытие, не было такого что тени от этого объекта перестают падать.
            foreach (var m in _hiddenMeshRenderers)
            {
                m.enabled = !value;
            }

            if (!value)
            {
                _hideCooldown = 0.1f;
            }
        }

        private void ResetObject()
        {
            ResetTransparency();
            
            Hide(false);
        }

        private void SetIgnoreRaycastLayer(bool value)
        {
            for (var i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].gameObject.layer = value ? LayerUtils.GetIgnoreRaycastLayer() : _layerCache[i];
            }
        }
    }
}
