using UnityEngine.UI.Extensions;

namespace SP.Runtime.Core.UI.Utilities
{
    public class Mask : SoftMaskScript
    {
        public void SetActiveMask(bool value)
        {
            CutOff = value ? 0 : 1;
            FlipAlphaMask = !value;
            DontClipMaskScalingRect = !value;
        }
    }
}