using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SP.Runtime.Core.Entities.Player
{
    public class SkinColorHandler : NetworkBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Material _sharedBodyMaterial;
        [SerializeField] private Color[] _bodyColorOptions;
        
        [SerializeField] private Material _sharedHairMaterial;
        [SerializeField] private Color[] _hairColorOptions;
        
        [SerializeField] private Material _sharedUnderwearMaterial;
        [SerializeField] private Color[] _underwearColorOptions;

        [SyncVar(hook = nameof(OnBodyColorIndexUpdated))] private int _bodyColorIndex = -1;
        [SyncVar(hook = nameof(OnHairColorIndexUpdated))] private int _hairColorIndex = -1;
        [SyncVar(hook = nameof(OnUnderwearColorIndexUpdated))] private int _underwearColorIndex = -1;

        private Material _bodyMaterial;
        private Material _hairMaterial;
        private Material _underwearMaterial;
        
        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            var renderers = GetComponentsInChildren<Renderer>();

            foreach (var r in renderers)
            {
                var sharedMaterials = r.sharedMaterials;
                
                var materials = r.materials;
                
                for (var i = 0; i < sharedMaterials.Length; i++)
                {
                    if (sharedMaterials[i] == _sharedBodyMaterial)
                    {
                        _bodyMaterial = materials[i];
                        continue;
                    }

                    if (sharedMaterials[i] == _sharedHairMaterial)
                    {
                        _hairMaterial = materials[i];
                        continue;
                    }

                    if (sharedMaterials[i] == _sharedUnderwearMaterial)
                    {
                        _underwearMaterial = materials[i];
                    }
                }
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _bodyColorIndex = _bodyColorOptions.Length == 0 ? -1 : Random.Range(0, _bodyColorOptions.Length);
            _hairColorIndex = _hairColorOptions.Length == 0 ? -1 : Random.Range(0, _hairColorOptions.Length);
            _underwearColorIndex = _underwearColorOptions.Length == 0 ? -1 : Random.Range(0, _underwearColorOptions.Length);
        }
        
        private void OnBodyColorIndexUpdated(int oldValue, int newValue)
        {
            UpdateMaterial();
        }
        
        private void OnHairColorIndexUpdated(int oldValue, int newValue)
        {
            UpdateMaterial();
        }
        
        private void OnUnderwearColorIndexUpdated(int oldValue, int newValue)
        {
            UpdateMaterial();
        }
        
        private void UpdateMaterial()
        {
            if (_bodyColorIndex > -1)
            {
                _bodyMaterial.color = _bodyColorOptions[_bodyColorIndex];
            }

            if (_hairColorIndex > -1)
            {
                _hairMaterial.color = _hairColorOptions[_hairColorIndex];
            }

            if (_underwearColorIndex > -1)
            {
                _underwearMaterial.color = _underwearColorOptions[_underwearColorIndex];
            }
        }
    }
}