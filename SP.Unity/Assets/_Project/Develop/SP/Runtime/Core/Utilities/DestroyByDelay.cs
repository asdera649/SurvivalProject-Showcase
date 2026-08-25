using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Utilities
{
    public class DestroyByDelay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _delay = 1;
        [SerializeField] private bool _isPool;

        private void OnEnable()
        {
            Invoke(nameof(Destroy), _delay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Destroy));
        }

        private void Destroy()
        {
            if (!_isPool)
            {
                Destroy(gameObject);
            }
            else
            {
                Loader.Instance.PoolService.ReleaseObject(gameObject);
            }
        }
    }
}
