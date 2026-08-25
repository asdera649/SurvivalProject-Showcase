using System;
using System.Linq;
using UnityEngine;

namespace SP.Runtime.Core.Items.Settings.Impact
{
    [CreateAssetMenu(menuName = "Items/Impact/SoundImpactSettings")]
    public class SoundImpactSettings : ScriptableObject
    {
        #region Structs

        public enum Surface
        {
            Grass = 0,
            Tree,
            Ore,
            Wood,
            Rock,
            Metal,
            Water,
            Character,
            StoneChunk
        }

        [Serializable]
        public class SurfaceTextures
        {
            [SerializeField] private Texture2D[] _textures;
            public Texture2D[] Textures => _textures;
            
            [SerializeField] private Surface _surface;
            public Surface Surface => _surface;
        }
        
        [Serializable]
        public class SurfaceMaterials
        {
            [SerializeField] private Material[] _materials;
            public Material[] Materials => _materials;
            
            [SerializeField] private Surface _surface;
            public Surface Surface => _surface;
        }
        
        #endregion
        
        [Header("Settings")]
        [SerializeField] private SurfaceTextures[] _surfaceTextures;
        [SerializeField] private SurfaceMaterials[] _surfaceMaterials;
        
        public bool TryGetSurfaceByMaterial(Material material, out Surface output)
        {
            output = Surface.Grass;
            
            foreach (var s in _surfaceMaterials)
            {
                if (s.Materials.Contains(material))
                {
                    output = s.Surface;
                    return true;
                }
            }

            if (HasMainTexture(material))
            {
                foreach (var s in _surfaceTextures)
                {
                    if (s.Textures.Contains(material.mainTexture))
                    {
                        output = s.Surface;
                        return true;
                    }
                }
            }

            foreach (var s in _surfaceMaterials)
            {
                if (s.Materials.Any(m => material.name.Contains(m.name)))
                {
                    output = s.Surface;
                    return true;
                }
            }

            return false;
        }
        
        private bool HasMainTexture(Material material)
        {
            return material.HasTexture("_MainTex");
        }
    }
}