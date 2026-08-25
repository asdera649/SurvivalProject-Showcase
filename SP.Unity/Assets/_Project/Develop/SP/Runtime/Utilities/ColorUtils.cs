using UnityEngine;

namespace SP.Runtime.Utilities
{
    public static class ColorUtils
    {
        public static Color ChangeColorAlpha(Color color, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}