using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Core.Systems.GroundColorMapRendering
{
    [ExecuteInEditMode]
    public class GroundColorMapRenderer : MonoBehaviour
    {
        private enum Resolution : int
        {
            _64x64 = 64,
            _128x128 = 128,
            _256x256 = 256,
            _512x512 = 512,
            _1024x1024 = 1024,
            _2048x2048 = 2048,
            _4096x4096 = 4096
        }

        [SerializeField] private List<GameObject> _groundObjects = new ();
        public IReadOnlyList<GameObject> GroundObjects => _groundObjects;
    
        public Bounds ColorMapBounds;
    
        [Space(10)]
    
        [SerializeField] private Resolution _resolution = Resolution._1024x1024;
        public int ColorMapResolution => (int)_resolution;
    
        [SerializeField] private LayerMask _cullingMask = -1;
        public LayerMask CullingMask => _cullingMask;
    
        [HideInInspector] public Vector4 ColorMapUv;
    
        public void RecalculateBounds()
        {
            ColorMapBounds = GroundColorMapRendering.GetTerrainBounds(_groundObjects);
            ColorMapUv = GroundColorMapRendering.BoundsToUV(ColorMapBounds);
        }
    
        public void Render()
        {
            GroundColorMapRendering.Render(this);
        }
    
        private void OnDrawGizmosSelected()
        {
            Color32 color = new Color(0f, 0.66f, 1f, 0.25f);
        
            Gizmos.color = color;
            Gizmos.DrawCube(ColorMapBounds.center, ColorMapBounds.size);

            color = new Color(0f, 0.66f, 1f, 1f);
        
            Gizmos.color = color;
            Gizmos.DrawWireCube(ColorMapBounds.center, ColorMapBounds.size);
        }
    }
}
