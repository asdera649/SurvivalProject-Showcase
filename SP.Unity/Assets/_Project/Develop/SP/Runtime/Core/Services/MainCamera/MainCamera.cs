using FMODUnity;
using SP.Runtime.Core.Camera;
using SP.Runtime.Core.Systems.Building.Builder;
using UnityEngine;

namespace SP.Runtime.Core.Services.MainCamera
{
    [RequireComponent(typeof(UnityEngine.Camera), typeof(StudioListener), typeof(Builder))]
    [RequireComponent(typeof(CameraFollow), typeof(ShakeHandler))]
    public class MainCamera : MonoBehaviour
    {
        private UnityEngine.Camera _camera;
        public UnityEngine.Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponent<UnityEngine.Camera>();
                }

                return _camera;
            }
        }

        private StudioListener _studioListener;
        public StudioListener StudioListener
        {
            get
            {
                if (_studioListener == null)
                {
                    _studioListener = GetComponent<StudioListener>();
                }

                return _studioListener;
            }
        }
        
        private Builder _builder;
        public Builder Builder
        {
            get
            {
                if (_builder == null)
                {
                    _builder = GetComponent<Builder>();
                }

                return _builder;
            }
        }

        private CameraFollow _cameraFollow;
        public CameraFollow CameraFollow
        {
            get
            {
                if (_cameraFollow == null)
                {
                    _cameraFollow = GetComponent<CameraFollow>();
                }

                return _cameraFollow;
            }
        }
        
        private ShakeHandler _shakeHandler;
        public ShakeHandler ShakeHandler
        {
            get
            {
                if (_shakeHandler == null)
                {
                    _shakeHandler = GetComponent<ShakeHandler>();
                }

                return _shakeHandler;
            }
        }
    }
}
