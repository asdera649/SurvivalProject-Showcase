using UnityEngine;

namespace SP.Runtime.Utilities
{
    public static class LayerUtils
    {
        public static int GetIgnoreRaycastLayer()
        {
            return LayerMask.NameToLayer("Ignore Raycast");
        }
        
        public static int GetGroundLayer()
        {
            return LayerMask.NameToLayer("Ground");
        }

        public static int GetWaterLayer()
        {
            return LayerMask.NameToLayer("Water");
        }
        
        public static int GetWaterLayerMask()
        {
            return LayerMask.GetMask("Water");
        }
    }
}