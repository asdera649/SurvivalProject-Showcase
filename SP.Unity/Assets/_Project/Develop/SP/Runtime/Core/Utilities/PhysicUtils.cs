using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Component = UnityEngine.Component;

namespace SP.Runtime.Core.Utilities
{
    public static class PhysicUtils
    {
        #region Structs

        public readonly struct CastResult<T> where T : class
        {
            public CastResult(T obj, List<RaycastHit> hits)
            {
                Object = obj;
                Hits = hits;
            }
            
            public T Object { get; }
            public List<RaycastHit> Hits { get; }
        }
        
        public readonly struct Result<T> where T: class
        {
            public Result(T obj, List<Collider> colliders)
            {
                Object = obj;
                Colliders = colliders;
            }
            
            public T Object { get; }
            public List<Collider> Colliders { get; }
            
            public Bounds Bounds
            {
                get
                {
                    var output = Colliders.Count != 0 ? Colliders[0].bounds : new Bounds(Vector3.zero, Vector3.zero);

                    foreach (var c in Colliders)
                    {
                        output.Encapsulate(c.bounds);
                    }
                    
                    return output;
                }
            }

            public Vector3 GetClosestPoint(Vector3 position)
            {
                var output = position;
                var distance = float.MaxValue;
                
                foreach (var c in Colliders)
                {
                    var closestPoint = PhysicUtils.GetClosestPoint(c, position);
                    var currentDistance = Vector3.Distance(position, closestPoint);

                    if (currentDistance < distance)
                    {
                        output = closestPoint;
                        distance = currentDistance;
                    }
                }

                return output;
            }
        }
        
        #endregion

        public static IReadOnlyList<CastResult<T>> GetSphereCastAll<T>(
            Vector3 origin,
            float radius,
            Vector3 direction, 
            [DefaultValue("Mathf.Infinity")] float maxDistance, 
            [DefaultValue("DefaultRaycastLayers")] int layerMask, 
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction) where T: class
        {
            var outputs = new List<CastResult<T>>();
            
            var hits = Physics.SphereCastAll(
                origin,
                radius,
                direction,
                maxDistance, 
                layerMask,
                queryTriggerInteraction).OrderBy(h => h.distance);
            
            foreach (var h in hits)
            {
                var isContains = false;
                
                var type = h.transform.GetComponentInParent<T>();

                if (type == null)
                {
                    continue;
                }

                foreach (var o in outputs)
                {
                    if (o.Object == type)
                    {
                        o.Hits.Add(h);
                        
                        isContains = true;
                        
                        break;
                    }
                }

                if (!isContains)
                {
                    outputs.Add(new CastResult<T>(type, new List<RaycastHit>() { h }));
                }
            }

            return outputs;
        }

        public static IReadOnlyList<Result<T>> GetBySphere<T>(
            Vector3 position, 
            float radius, 
            [DefaultValue("AllLayers")] int layerMask, 
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction) where T: class
        {
            var outputs = new List<Result<T>>();
            
            var colliders = Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);

            foreach (var c in colliders)
            {
                var isContains = false;
                
                var type = c.GetComponentInParent<T>();

                if (type == null)
                {
                    continue;
                }

                foreach (var o in outputs)
                {
                    if (o.Object == type)
                    {
                        o.Colliders.Add(c);
                        
                        isContains = true;
                        
                        break;
                    }
                }

                if (!isContains)
                {
                    outputs.Add(new Result<T>(type, new List<Collider>() { c }));
                }
            }

            return outputs;
        }

        public static IReadOnlyList<T> GetTypesBySphere<T>(
            Vector3 position, 
            float radius, 
            [DefaultValue("AllLayers")] int layerMask, 
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            var outputs = new List<T>();
            
            var colliders = Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);

            Collect(colliders, ref outputs);

            return outputs;
        }
        
        public static IReadOnlyList<T> GetTypesByBox<T>(
            Vector3 center, 
            Vector3 halfExtents, 
            [DefaultValue("Quaternion.identity")] Quaternion orientation, 
            [DefaultValue("AllLayers")] int layerMask, 
            [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            var outputs = new List<T>();
            
            var colliders = Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);

            Collect(colliders, ref outputs);
            
            return outputs;
        }

        public static Vector3 GetClosestPoint(Collider collider, Vector3 position)
        {
            MeshCollider meshCollider = null;

            if (collider is MeshCollider { convex: false } mesh )
            {
                meshCollider = mesh;
                meshCollider.convex = true;
            }
                    
            var output = collider.ClosestPoint(position);

            if (meshCollider != null)
            {
                meshCollider.convex = false;
            }

            return output;
        }

        #region Utilities

        private static void Collect<T>(IReadOnlyList<Component> components, ref List<T> outputs)
        {
            foreach (var c in components)
            {
                var type = c.GetComponentInParent<T>();

                if (type == null)
                {
                    continue;
                }

                if (!outputs.Contains(type))
                {
                    outputs.Add(type);
                }
            }
        }

        #endregion
    }
}