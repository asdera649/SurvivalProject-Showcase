using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SP.Runtime.Core.Camera.Input
{
    public class BaseCameraInput : MonoBehaviour
    {
        public event UnityAction<Vector2> SwipeAction;
        public event UnityAction<Vector2, float> PinchAction;
        
        private readonly List<RaycastResult> _results = new();

        protected void InvokeSwipeAction(Vector2 deltaPosition)
        {
            SwipeAction?.Invoke(deltaPosition);
        }
        
        protected void InvokePinchAction(Vector2 center, float delta)
        {
            PinchAction?.Invoke(center, delta);
        }
        
        protected bool IsPointerOverUIObject(Vector2 position) 
        {
            if (EventSystem.current == null)
            {
                return true;
            }
            
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(
                    position.x,
                    position.y)
            };
            
            _results.Clear();
            
            EventSystem.current.RaycastAll(eventDataCurrentPosition, _results);
            
            return _results.Count > 0;
        }
    }
}