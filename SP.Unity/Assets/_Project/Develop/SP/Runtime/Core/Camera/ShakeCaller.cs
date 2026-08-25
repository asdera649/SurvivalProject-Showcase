using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Camera
{
    public class ShakeCaller : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _maxCoverageRadius;
        [SerializeField] private AnimationCurve _intensitySpread;

        private void OnEnable()
        {
            Shake();
        }

        private void Shake()
        {
            var handlers = PhysicUtils.GetTypesBySphere<ShakeHandler>(
                transform.position,
                _maxCoverageRadius,
                -1,
                QueryTriggerInteraction.Collide);

            foreach (var h in handlers)
            {
                var percentage = Mathf.Clamp01(
                    1 - Vector3.Distance(transform.position, h.transform.position) / _maxCoverageRadius);
                
                h.InduceStress(_intensitySpread.Evaluate(percentage));
            }
        }
    }
}