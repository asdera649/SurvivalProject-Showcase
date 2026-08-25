using System.Collections;
using SP.Runtime.Core.UI.UIControls.RadialMenu;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Interaction
{
    public class InteractionButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event UnityAction Clicked;

        [Header("References")] 
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _outline;
        [SerializeField] private Image _progressOutline;
        
        [SerializeField] private RadialMenu _radialMenu;
        public RadialMenu RadialMenu => _radialMenu;

        [Header("Settings")]
        [SerializeField] private float _dragThreshold;

        private bool _isHoldMode;
        
        private bool _isPointerDown;
        private bool _isDrag;
        private Coroutine _holdCoroutine;
        
        private Vector2 _dragStartPosition;

        public void Initialize(Sprite icon, bool holdMode = false)
        {
            _icon.sprite = icon;
            _isHoldMode = holdMode;
        }

        private void OnDestroy()
        {
            if (_holdCoroutine != null)
            {
                StopCoroutine(_holdCoroutine);
                _holdCoroutine = null;
            }
        }

        private void Update()
        {
            _background.gameObject.SetActive(!RadialMenu.IsOpen);
            _icon.gameObject.SetActive(!RadialMenu.IsOpen);
            _outline.SetActive(!(RadialMenu.IsOpen || _radialMenu.IsEmpty));
        }
    
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isPointerDown)
            {
                return;
            }
            
            _dragStartPosition = eventData.position;
            
            transform.localScale *= 0.9f;

            _isPointerDown = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isPointerDown)
            {
                return;
            }
            
            var distance = Vector2.Distance(_dragStartPosition, eventData.position);

            if (distance > _dragThreshold)
            {
                if (_isDrag == false)
                {
                    RadialMenu.Open();
                    transform.SetAsLastSibling();
                }

                RadialMenu.InputPosition = eventData.position - (Vector2)transform.position;
            
                _isDrag = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPointerDown)
            {
                return;
            }
            
            transform.localScale /= 0.9f;
            
            if (_isDrag)
            {
                RadialMenu.Execute();
                RadialMenu.Close();
            }
            else
            {
                if (_isHoldMode)
                {
                    _holdCoroutine ??= StartCoroutine(nameof(Hold), 0.75f);
                }
                else
                {
                    Clicked?.Invoke();
                }
            }

            _isPointerDown = false;
            _isDrag = false;
        }

        private IEnumerator Hold(float delay)
        {
            var maxDelay = delay;
            
            _progressOutline.fillAmount = 0;
            
            while (delay > 0)
            {
                delay -= Time.deltaTime;

                _progressOutline.fillAmount = 1 - (delay / maxDelay);

                yield return new WaitForEndOfFrame();
            }

            _progressOutline.fillAmount = 0;
            
            Clicked?.Invoke();

            _holdCoroutine = null;
            
            yield return null;
        }
    }
}
