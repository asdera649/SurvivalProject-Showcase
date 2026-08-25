using Cinemachine;
using FMODUnity;
using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    [RequireComponent(typeof(LineRenderer))]
    public class Bullet : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _bulletFlyEventEmitter;
        [SerializeField] private CinemachinePath _cinemachinePath;
        
        [Header("Settings")]
        [SerializeField] private float _speed = 30;
        
        private LineRenderer _lineRenderer;
        private LineRenderer LineRenderer
        {
            get
            {
                if (_lineRenderer == null)
                {
                    _lineRenderer = GetComponent<LineRenderer>();
                }

                return _lineRenderer;
            }
        }

        public void Initialize(Vector3 startPosition, Vector3 endPosition)
        {
            _bulletFlyEventEmitter.Play();
            
            LineRenderer.SetPosition(0, startPosition);
            LineRenderer.SetPosition(1, endPosition);

            _cinemachinePath.m_Waypoints[0].position = startPosition;
            _cinemachinePath.m_Waypoints[1].position = endPosition;
        }
        
        private void Update()
        {
            UpdateBulletFly();
        }

        private void UpdateBulletFly()
        {
            LineRenderer.SetPosition(
                0,
                Vector3.Lerp(
                LineRenderer.GetPosition(0),
                LineRenderer.GetPosition(1),
                _speed * Time.deltaTime));

            if (LineRenderer.GetPosition(0) == LineRenderer.GetPosition(1))
            {
                DestroyLine();
            }
        }

        private void DestroyLine()
        {
            LineRenderer.SetPosition(0, Vector3.zero);
            LineRenderer.SetPosition(1, Vector3.zero);
            
            _cinemachinePath.m_Waypoints[0].position = Vector3.zero;
            _cinemachinePath.m_Waypoints[1].position = Vector3.zero;

            Loader.Instance.PoolService.ReleaseObject(this);
        }
    }
}
