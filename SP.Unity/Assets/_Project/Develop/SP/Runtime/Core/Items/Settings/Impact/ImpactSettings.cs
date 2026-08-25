using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Items.Settings.Impact
{
    [CreateAssetMenu(menuName = "Items/Impact/ImpactSettings")]
    public class ImpactSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private VisualImpactSettings.ImpactContainer _defaultVisualImpact;
        [SerializeField] private VisualImpactSettings[] _visualImpactSettings;

        [Space(10)]
        
        [SerializeField] private SoundImpactSettings.Surface _defaultSoundImpactSurface;
        [SerializeField] private SoundImpactSettings[] _soundImpactSettings;

        private VisualImpactSettings.ImpactContainer GetVisualImpact(Material material)
        {
            var output = _defaultVisualImpact;

            foreach (var s in _visualImpactSettings)
            {
                if (s.TryGetImpactByMaterial(material, out var impact))
                {
                    output = impact;
                    break;
                }
            }
            
            return output;
        }
        
        private SoundImpactSettings.Surface GetSoundImpactSurface(Material material)
        {
            var output = _defaultSoundImpactSurface;

            foreach (var s in _soundImpactSettings)
            {
                if (s.TryGetSurfaceByMaterial(material, out var surface))
                {
                    output = surface;
                    break;
                }
            }
            
            return output;
        }
        
        public void SpawnDefaultImpacts(Vector3 position)
        {
            var impact = _defaultVisualImpact;
                        
            if (TrySpawn(impact.ImpactPrefab, position, Vector3.up, impact.DontRotate, out var output))
            {
                output.Play(_defaultSoundImpactSurface);
            }
        }
        
        public void SpawnImpactsByHits(RaycastHit[] hits)
        {
            foreach (var h in hits)
            {
                var meshRenderer = h.transform.GetComponentInChildren<Renderer>();
                
                if (meshRenderer != null)
                {
                    var impact = GetVisualImpact(meshRenderer.sharedMaterial);
                        
                    if (impact.DontSpawnForTrigger && h.collider.isTrigger)
                    {
                        continue;
                    }
                        
                    if (TrySpawn(impact.ImpactPrefab, h.point, h.normal, impact.DontRotate, out var output))
                    {
                        output.Play(GetSoundImpactSurface(meshRenderer.sharedMaterial));
                    }
                }
            }
        }
        
        private bool TrySpawn(
            Objects.Impact impactPrefab,
            Vector3 position,
            Vector3 direction,
            bool dontRotate,
            out Objects.Impact output)
        {
            return Loader.Instance.PoolService.TrySpawnObject(
                impactPrefab,
                position,
                dontRotate ? Quaternion.identity : Quaternion.LookRotation(direction.normalized, Vector3.forward),
                out output);
        }
    }
}
