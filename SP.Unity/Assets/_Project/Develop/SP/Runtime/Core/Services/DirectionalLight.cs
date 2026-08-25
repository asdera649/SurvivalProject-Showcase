using UnityEngine;

namespace SP.Runtime.Core.Services
{
    [RequireComponent(typeof(Light))]
    public class DirectionalLight : MonoBehaviour
    {
        private Light _light;
        public Light Light
        {
            get
            {
                if (_light == null)
                {
                    _light = GetComponent<Light>();
                }

                return _light;
            }
        }
    }
}
