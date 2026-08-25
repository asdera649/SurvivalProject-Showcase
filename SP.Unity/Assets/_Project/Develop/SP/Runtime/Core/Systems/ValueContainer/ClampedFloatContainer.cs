using UnityEngine;

namespace SP.Runtime.Core.Systems.ValueContainer
{
    public class ClampedFloatContainer : FloatContainer
    {
        public override float GetTotal()
        {
            var output = 0f;

            foreach (var t in total)
            {
                output += t.Value;
            }

            return Mathf.Clamp01(output);
        }
    }
}