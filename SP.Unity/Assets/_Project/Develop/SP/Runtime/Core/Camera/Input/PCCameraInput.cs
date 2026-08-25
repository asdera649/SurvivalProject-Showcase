using UnityEngine;

namespace SP.Runtime.Core.Camera.Input
{
    public class PCCameraInput : BaseCameraInput
    {
        [Header("Settings")]
        [SerializeField] private float _mouseScrollSpeed = 0.25f;
        
        private bool _isTouching;
        private Vector2 _touchLastPosition;
        
        private void Update()
        {
            UpdateInput();
        }

        private void UpdateInput() 
        {
            if (UnityEngine.Input.GetMouseButtonDown(0)) 
            {
                if (!IsPointerOverUIObject(UnityEngine.Input.mousePosition)) 
                {
                    _touchLastPosition = UnityEngine.Input.mousePosition;
                    _isTouching = true;
                }
            }

            if (UnityEngine.Input.GetMouseButton(0) && _isTouching) 
            {
                var deltaPosition = (Vector2)UnityEngine.Input.mousePosition - _touchLastPosition;
                
                _touchLastPosition = UnityEngine.Input.mousePosition;

                if (deltaPosition != Vector2.zero) 
                {
                    InvokeSwipeAction(deltaPosition);
                }
            }

            if (UnityEngine.Input.GetMouseButtonUp(0) && _isTouching) 
            {
                _isTouching = false;
            }
            
            if (UnityEngine.Input.mouseScrollDelta.y != 0)
            {
                InvokePinchAction(
                    UnityEngine.Input.mousePosition,
                    UnityEngine.Input.mouseScrollDelta.y < 0 ? -_mouseScrollSpeed : _mouseScrollSpeed);
            }
        }
    }
}