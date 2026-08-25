using System;
using System.Linq;
using UnityEngine;

namespace SP.Runtime.Core.Items.Settings.Impact
{
    [CreateAssetMenu(menuName = "Items/Impact/VisualImpactSettings")]
    public class VisualImpactSettings : ScriptableObject
    {
        #region Structs
        
        [Serializable]
        public class ImpactContainer
        {
            [SerializeField] private Objects.Impact _impactPrefab;
            public Objects.Impact ImpactPrefab => _impactPrefab;

            [SerializeField] private bool _dontRotate;
            public bool DontRotate => _dontRotate;

            [SerializeField] private bool _dontSpawnForTrigger = true;
            public bool DontSpawnForTrigger => _dontSpawnForTrigger;
        }
        
        [Serializable]
        public class ImpactTextures
        {
            [SerializeField] private Texture2D[] _textures;
            public Texture2D[] Textures => _textures;
            
            [SerializeField] private ImpactContainer _impact;
            public ImpactContainer Impact => _impact;
        }
        
        [Serializable]
        public class ImpactMaterials
        {
            [SerializeField] private Material[] _materials;
            public Material[] Materials => _materials;
            
            [SerializeField] private ImpactContainer _impact;
            public ImpactContainer Impact => _impact;
        }
        
        #endregion
        
        [Header("Settings")]
        [SerializeField] private ImpactTextures[] _impactTextures;
        [SerializeField] private ImpactMaterials[] _impactMaterials;
        
        public bool TryGetImpactByMaterial(Material material, out ImpactContainer output)
        {
            output = null;
            
            foreach (var i in _impactMaterials)
            {
                if (i.Materials.Contains(material))
                {
                    output = i.Impact;
                    return true;
                }
            }
            
            foreach (var i in _impactTextures)
            {
                if (i.Textures.Contains(material.mainTexture))
                {
                    output = i.Impact;
                    return true;
                }
            }
            
            foreach (var i in _impactMaterials)
            {
                if (i.Materials.Any(m => material.name.Contains(m.name)))
                {
                    output = i.Impact;
                    return true;
                }
            }

            return false;
        }
    }
}