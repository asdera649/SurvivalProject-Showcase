using UnityEngine;

namespace SP.Runtime.Core.Utilities
{
    public static class ObjectUtils
    {
        public static Vector3 CalculateCenter(params Transform[] objects)
        {
            var output = new Bounds();
            
            foreach (var o in objects)
            {
                var renderers = o.GetComponentsInChildren<Renderer>();
                
                foreach (var r in renderers)
                {
                    if (r is ParticleSystemRenderer)
                    {
                        continue;
                    }
                    
                    if (output.size == Vector3.zero)
                    {
                        output = r.bounds;
                    }
                    else
                    {
                        output.Encapsulate(r.bounds);
                    }
                }
                
                var colliders = o.GetComponentsInChildren<Collider>();
                
                foreach (var c in colliders)
                {
                    if (output.size == Vector3.zero)
                    {
                        output = c.bounds;
                    }
                    else
                    {
                        output.Encapsulate(c.bounds);
                    }
                }
            }
            
            return output.center;
        }

        public static Bounds GetBounds(GameObject gameObject)
        {
            Bounds bounds = new(gameObject.transform.childCount > 0 ? 
                gameObject.transform.GetChild(0).transform.position : gameObject.transform.position, Vector3.zero);

            foreach (var r in gameObject.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }
    }
}