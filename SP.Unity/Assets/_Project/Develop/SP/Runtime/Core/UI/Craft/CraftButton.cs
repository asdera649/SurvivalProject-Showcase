using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Craft
{
    [RequireComponent(typeof(Button), typeof(Animation))]
    public class CraftButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event UnityAction HoldCompleted;

        [Header("References")] 
        [SerializeField] private Image _hover;
        [SerializeField] private StudioEventEmitter _craftProcessEventEmitter;
        [SerializeField] private StudioEventEmitter _craftCompletedEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private float _holdTime = 0.65f;

        private bool _isHold;
        private bool IsHold
        {
            get => _isHold;
            set
            {
                var oldValue = _isHold;
                _isHold = value;

                if (oldValue == _isHold)
                {
                    return;
                }
                
                OnIsHoldUpdated(oldValue, _isHold);
            }
        }
        
        private float _holdProgress;
        private float HoldProgress
        {
            get => _holdProgress;
            set
            {
                var oldValue = _holdProgress;
                _holdProgress = Mathf.Clamp01(value);

                if (oldValue == _holdProgress)
                {
                    return;
                }
                
                OnHoldProgressUpdated(oldValue, _holdProgress);
            }
        }
        
        private Button _button;
        public Button Button
        {
            get
            {
                if (_button == null)
                {
                    _button = GetComponent<Button>();
                }

                return _button;
            }
        }

        private Animation _animation;
        private Animation Animation
        {
            get
            {
                if (_animation == null)
                {
                    _animation = GetComponent<Animation>();
                }

                return _animation;
            }
        }
        
        private void Update()
        {
            UpdateHold();
        }

        private void UpdateHold()
        {
            if (!Button.interactable)
            {
                IsHold = false;
                HoldProgress = 0;
                return;
            }
            
            if (IsHold)
            {
                HoldProgress += _holdTime * Time.deltaTime;
                
                if (HoldProgress >= 1)
                {
                    CompleteHold();
                    
                    IsHold = false;
                }
            }
            else
            {
                HoldProgress = 0;
            }
        }

        private void CompleteHold()
        {
            _craftCompletedEventEmitter.Play();
            
            Animation.Play();
                    
            HoldCompleted?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHold = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            IsHold = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHold = false;
        }

        #region Callbacks

        private void OnIsHoldUpdated(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                if (!_craftProcessEventEmitter.IsPlaying())
                {
                    _craftProcessEventEmitter.Play();
                }
            }
            else
            {
                if (_craftProcessEventEmitter.IsPlaying())
                {
                    _craftProcessEventEmitter.Stop();
                }
            }
        }

        private void OnHoldProgressUpdated(float oldValue, float newValue)
        {
            _hover.fillAmount = newValue;
        }

        #endregion
    }
}