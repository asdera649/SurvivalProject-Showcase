using UnityEngine;

namespace SP.Runtime.Core.Utilities
{
    public static class LODUtils
    {
        public static float DistanceToRelativeHeight(UnityEngine.Camera camera, float distance, float size)
        {
            if (camera.orthographic)
            {
                return size * 0.5F / camera.orthographicSize;
            }

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            
            return relativeHeight;
        }
    }
}