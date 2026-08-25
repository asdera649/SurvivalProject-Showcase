using UnityEngine;

namespace SP.Runtime.Core.UI.MiniMap.Input
{
    public class PCMapInput : BaseMapInput
    {
        [Header("Settings")]
        [SerializeField] private float _mouseScrollSpeed = 0.25f;
        [SerializeField] private float _maxDistanceForPointerUp = 40;
        [SerializeField] private float _maxDurationForPointerUp = 0.4f;
        
        private bool _isTouching;
        
        private Vector2 _touchStartPosition;
        private Vector2 _touchLastPosition;
        private float _touchStartTime;
        
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
                    _touchStartPosition = UnityEngine.Input.mousePosition;
                    _touchStartTime = Time.time;
                    _touchLastPosition = _touchStartPosition;
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
                if (Time.time - _touchStartTime <= _maxDurationForPointerUp &&
                    Vector2.Distance(UnityEngine.Input.mousePosition, _touchStartPosition) <= _maxDistanceForPointerUp) 
                {
                    InvokePointerUpAction(UnityEngine.Input.mousePosition);
                }
                
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