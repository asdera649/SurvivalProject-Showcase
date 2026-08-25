namespace SP.Runtime.Core.Systems.ValueContainer
{
    public class BoolContainer : Container<bool>
    {
        public override bool GetTotal()
        {
            foreach (var t in total)
            {
                if (t.Value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}