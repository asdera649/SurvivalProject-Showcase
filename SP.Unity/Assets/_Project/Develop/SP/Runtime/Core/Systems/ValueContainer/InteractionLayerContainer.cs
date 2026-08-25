using SP.Runtime.Core.Systems.Interaction;

namespace SP.Runtime.Core.Systems.ValueContainer
{
    public class InteractionLayerContainer : Container<InteractionSeeker.InteractionLayer>
    {
        public override InteractionSeeker.InteractionLayer GetTotal()
        {
            return InteractionSeeker.InteractionLayer.Default;
        }
    }
}