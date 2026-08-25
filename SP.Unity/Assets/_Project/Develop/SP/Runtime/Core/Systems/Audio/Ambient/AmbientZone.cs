using UnityEngine;

namespace SP.Runtime.Core.Systems.Audio.Ambient
{
    public class AmbientZone : BaseAmbient
    {
        [SerializeField] private bool _invert;
        
        private void Update()
        {
            UpdateCart();
        }

        private void UpdateCart()
        {
            if (TryGetListener(out var listener))
            {
                SetCartPosition();

                if ((_invert ? -1 : 1) * Vector3.Dot(transform.position - listener.position, transform.right) < 0)
                {
                    transform.position = new Vector3(
                        listener.position.x,
                        transform.position.y, // положение источника звука зависит от его начального положения на карте //listener.position.y + 1,
                        listener.position.z);
                }
            }
        }
    }
}