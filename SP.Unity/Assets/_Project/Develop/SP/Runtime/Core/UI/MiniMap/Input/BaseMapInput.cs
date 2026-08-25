using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SP.Runtime.Core.UI.MiniMap.Input
{
    public class BaseMapInput : MonoBehaviour
    {
        public event UnityAction<Vector2> PointerDownAction;
        public event UnityAction<Vector2> PointerUpAction;
        public event UnityAction<Vector2> SwipeAction;
        public event UnityAction<Vector2, float> PinchAction;
        
        private readonly List<RaycastResult> _results = new();

        protected void InvokePointerDownAction(Vector2 position)
        {
            PointerDownAction?.Invoke(position);
        }
        
        protected void InvokePointerUpAction(Vector2 position)
        {
            PointerUpAction?.Invoke(position);
        }
        
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

            foreach (var r in _results)
            {
                if (r.gameObject == gameObject)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}