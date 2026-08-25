using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Systems.Audio.Footstep
{
    [CreateAssetMenu(menuName = "Footstep/FootstepSurfaceSettings")]
    public class FootstepSurfaceSettings : ScriptableObject
    {
        #region Structs

        public enum Surface
        {
            Grass = 0,
            Sand,
            Water,
            Wood,
            Rock,
            Metal
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
        [SerializeField] private Surface _defaultSurface;
        
        [FormerlySerializedAs("_terrainSurfaces")]
        [SerializeField] private SurfaceTextures[] _surfaceTextures;
        
        [FormerlySerializedAs("_surfaces")] 
        [SerializeField] private SurfaceMaterials[] _surfaceMaterials;

        public Surface GetSurfaceByTexture(Texture2D texture)
        {
            var output = _defaultSurface;

            foreach (var s in _surfaceTextures)
            {
                if (s.Textures.Contains(texture))
                {
                    output = s.Surface;
                    break;
                }
            }

            return output;
        }

        public Surface GetSurfaceByMaterial(Material material)
        {
            var output = _defaultSurface;

            foreach (var s in _surfaceMaterials)
            {
                if (s.Materials.Contains(material))
                {
                    output = s.Surface;
                    return output;
                }
            }

            if (HasMainTexture(material))
            {
                foreach (var s in _surfaceTextures)
                {
                    if (s.Textures.Contains(material.mainTexture))
                    {
                        output = s.Surface;
                        break;
                    }
                }
            }

            return output;
        }

        private bool HasMainTexture(Material material)
        {
            return material.HasTexture("_MainTex");
        }
    }
}