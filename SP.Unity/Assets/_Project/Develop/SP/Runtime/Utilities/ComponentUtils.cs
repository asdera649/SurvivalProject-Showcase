using UnityEngine;

namespace SP.Runtime.Utilities
{
    public static class ComponentUtils
    {
        public static bool TryGetComponentInParent<T>(Component component, out T output) where T: class
        {
            output = null;

            output = component.GetComponentInParent<T>();

            return output != null;
        }
    }
}