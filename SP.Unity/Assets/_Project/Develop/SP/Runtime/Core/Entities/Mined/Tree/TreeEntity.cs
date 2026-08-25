using System;
using FMODUnity;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Destruction;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SP.Runtime.Core.Entities.Mined.Tree
{
    public class TreeEntity : MinedEntity
    {
        #region Structs

        [Serializable]
        private struct TreeColor
        {
            [Range(0, 100)]
            public int Probability;
            public Color Color;
            public Color CoverageColor;
        }

        #endregion
        
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private StudioEventEmitter _treeFallingEventEmitter;
        
        [Header("Settings")] 
        [SerializeField] private Vector2 _randomAnimationSpeedForSwaying = new (0.7f, 1);

        [Header("Color")] 
        [SerializeField] private MeshRenderer[] _meshRenderers;
        
        [Space(10)]
        
        [SerializeField] private uint _branchMaterialIndex;
        
        [Space(10)]
        
        [SerializeField] private Material _materialNormal;
        [SerializeField] private Material _materialDark;
        [SerializeField] private Material _materialLight;
        
        [Space(10)]
        
        [Tooltip("Масштаб шума. Больше = крупнее кластеры.")]
        [SerializeField] private float _noiseScale = 0.06f;

        [Tooltip("Смещение seed по X. Меняй для разных результатов.")]
        [SerializeField] private float _noiseSeedX = 362.2f;

        [Tooltip("Смещение seed по Y (Z в world space).")]
        [SerializeField] private float _noiseSeedY = 229.6f;
        
        [Space(10)]
        
        [SerializeField] private float _thresholdLight = 0.724f;
        [SerializeField] private float _thresholdDark  = 0.437f;
        
        //[SerializeField] private TreeColor[] _branchRandomColors;
        
        private Material _branchMaterial;
        
        private readonly int _hit = Animator.StringToHash("Hit");
        private readonly int _swayingSpeed = Animator.StringToHash("SwayingSpeed");

        protected override void Awake()
        {
            base.Awake();
            
            _animator.SetFloat(
                _swayingSpeed,
                Random.Range(_randomAnimationSpeedForSwaying.x, _randomAnimationSpeedForSwaying.y));
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            UpdateBranchColor();
        }

        private void UpdateBranchColor()
        {
            _branchMaterial = SampleMaterial(transform.position);
            
            foreach (var m in _meshRenderers)
            {
                if (m.sharedMaterials.Length <= _branchMaterialIndex)
                {
                    continue;
                }

                var mat = m.sharedMaterials;

                mat[_branchMaterialIndex] = _branchMaterial;

                m.sharedMaterials = mat;
            }
        }
        
        /// <summary>
        /// Сэмплирует Perlin Noise и возвращает материал.
        /// Шум даёт значение [0..1]. Близкие объекты дают близкие значения
        /// соседние кусты образуют цветовые кластеры.
        /// </summary>
        private Material SampleMaterial(Vector3 worldPos)
        {
            var nx = (worldPos.x + _noiseSeedX) * _noiseScale;
            var ny = (worldPos.z + _noiseSeedY) * _noiseScale; // Z — горизонтальная ось

            var noise = Mathf.PerlinNoise(nx, ny); // [0..1]

            if (noise >= _thresholdLight)
            {
                return _materialLight;
            }
            
            return noise >= _thresholdDark ? _materialDark : _materialNormal;
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            base.OnDeath();

            // Если это хост, то просто вызываем Destruct().
            // Все потому, что если у хоста перед уничтожением отправить Rpc,то он, не отправится.
            
            if (isServer && isClient)
            {
                Destruct();
            }
            
            RpcDestruct();
        }

        [ClientRpc]
        private void RpcDestruct()
        {
            Destruct();
        }

        private void Destruct()
        {
            _treeFallingEventEmitter.Play();
            
            if (NetworkClient.localPlayer == null || !TryGetComponent(out DestructionHandler dh))
            {
                return;
            }
            
            var debrisPieces = dh.Destruct(
                NetworkClient.localPlayer.transform.position - transform.position);

            if (_branchMaterial == null)
            {
                return;
            }
            
            foreach (var d in debrisPieces)
            {
                if (d is TreeDebrisPiece t)
                {
                    t.SetMaterial(_branchMaterial);
                }
            }
        }

        [ServerCallback]
        protected override void OnTakeDamage(DamageSenderInfo sender, int damage, DeathMethods deathMethod)
        {
            base.OnTakeDamage(sender, damage, deathMethod);
            
            RpcShowImpact();
        }
        
        [ClientRpc]
        private void RpcShowImpact()
        {
            _animator.ResetTrigger(_hit);
            _animator.SetTrigger(_hit);
        }
    }
}
