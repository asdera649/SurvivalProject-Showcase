using System.ComponentModel;
using System.Linq;
using SP.Runtime.Core.Utilities;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Utilities
{
    public static class BuildUtils
    {
        public static bool Linecast(
            Vector3 start,
            Vector3 end,
            out RaycastHit hitInfo,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            hitInfo = default;

            var distance = Vector3.Distance(start, end);
            
            if (distance < 0.01f)
            {
                return false;
            }
            
            var hits = Physics.RaycastAll(
                start,
                end - start,
                distance,
                layerMask,
                queryTriggerInteraction).OrderBy(h => h.distance);
            
            foreach (var h in hits)
            {
                if (ComponentUtils.TryGetComponentInParent<Obstacle>(h.transform, out _))
                {
                    hitInfo = h;
                    return true;
                }
            }
            
            return false;
        }
        
        public static bool Linecast(
            Vector3 start,
            Vector3 end,
            float radius,
            out RaycastHit hitInfo,
            [DefaultValue("DefaultRaycastLayers")] int layerMask,
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            hitInfo = default;

            var distance = Vector3.Distance(start, end);
            
            if (distance < 0.01f)
            {
                return false;
            }
            
            var hits = Physics.SphereCastAll(
                start,
                radius,
                end - start,
                distance,
                layerMask,
                queryTriggerInteraction).OrderBy(h => h.distance);
            
            foreach (var h in hits)
            {
                if (ComponentUtils.TryGetComponentInParent<Obstacle>(h.transform, out _))
                {
                    hitInfo = h;
                    return true;
                }
            }
            
            return false;
        }

        public static bool CheckSphere(
            Vector3 position,
            float radius,
            out Vector3 hitPoint,
            [UnityEngine.Internal.DefaultValue("AllLayers")] int layerMask,
            [UnityEngine.Internal.DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            hitPoint = Vector3.zero;
            
            var colliders = Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);

            var distance = float.MaxValue;
            
            foreach (var c in colliders)
            {
                if (ComponentUtils.TryGetComponentInParent<Obstacle>(c, out _))
                {
                    var point = PhysicUtils.GetClosestPoint(c, position);
                    var currentDistance = Vector3.Distance(position, point);

                    if (currentDistance < distance)
                    {
                        hitPoint = point;
                        distance = currentDistance;
                    }
                }
            }

            return distance != float.MaxValue;
        }
    }
} 