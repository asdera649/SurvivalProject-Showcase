using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SP.Runtime.Core.UI.MiniMap.Input
{
    public class MobileMapInput : BaseMapInput
    {
        [Header("Settings")] 
        [SerializeField] private float _pinchSpeed = 0.003f;
        [SerializeField] private float _maxDistanceForPointerUp = 40;
        [SerializeField] private float _maxDurationForPointerUp = 0.4f;

        private readonly Dictionary<int, Touch> _touchesCache = new();

        private bool _isTouching;
        
        private Vector2 _touchStartPosition;
        private float _touchStartTime;

        private void Update()
        {
            UpdateInput();
        }

        private void UpdateInput()
        {
            var touches = UnityEngine.Input.touches;
            
            foreach (var t in touches)
            {
                if (_touchesCache.ContainsKey(t.fingerId))
                {
                    _touchesCache[t.fingerId] = t;
                }
                else if (t.phase == TouchPhase.Began && !IsPointerOverUIObject(t.position))
                {
                    _touchesCache.Add(t.fingerId, t);
                }
            }

            if (_touchesCache.Count == 1)
            {
                var touch = _touchesCache.ElementAt(0).Value;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                    {
                        _touchStartPosition = touch.position;
                        _touchStartTime = Time.time;
                        
                        _isTouching = true;

                        break;
                    }
                    case TouchPhase.Moved:
                    {
                        if (touch.deltaPosition != Vector2.zero && _isTouching)
                        {
                            InvokeSwipeAction(touch.deltaPosition);
                        }

                        break;
                    }
                    case TouchPhase.Ended:
                    {
                        if (Time.time - _touchStartTime <= _maxDurationForPointerUp &&
                            Vector2.Distance(touch.position, _touchStartPosition) <= _maxDistanceForPointerUp &&
                            _isTouching) 
                        {
                            InvokePointerUpAction(touch.position);
                        }
                        
                        _isTouching = false;

                        break;
                    }
                    case TouchPhase.Stationary:
                    {
                        break;
                    }
                    case TouchPhase.Canceled:
                    {
                        break;
                    }
                }
            }
            else if (_touchesCache.Count == 2)
            {
                var touch0 = _touchesCache.ElementAt(0).Value;
                var touch1 = _touchesCache.ElementAt(1).Value;

                if (touch0.phase != TouchPhase.Ended && touch1.phase != TouchPhase.Ended)
                {
                    _isTouching = true;

                    var previousDistance = Vector2.Distance(
                        touch0.position - touch0.deltaPosition,
                        touch1.position - touch1.deltaPosition);

                    var currentDistance = Vector2.Distance(
                        touch0.position,
                        touch1.position);

                    if (previousDistance != currentDistance)
                    {
                        InvokePinchAction(
                            (touch0.position + touch1.position) / 2,
                            (currentDistance - previousDistance) * _pinchSpeed);
                    }
                }
            }
            else
            {
                if (_isTouching)
                {
                    _isTouching = false;
                }
            }
            
            foreach (var t in touches)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (_touchesCache.ContainsKey(t.fingerId))
                    {
                        _touchesCache.Remove(t.fingerId);
                    }
                }
            }
        }
    }
}