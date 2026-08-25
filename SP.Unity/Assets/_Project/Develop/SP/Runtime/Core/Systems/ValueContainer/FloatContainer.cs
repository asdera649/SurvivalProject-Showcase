namespace SP.Runtime.Core.Systems.ValueContainer
{
    public class FloatContainer : Container<float>
    {
        public override float GetTotal()
        {
            var output = 0f;

            foreach (var t in total)
            {
                output += t.Value;
            }

            return output;
        }
    }
}