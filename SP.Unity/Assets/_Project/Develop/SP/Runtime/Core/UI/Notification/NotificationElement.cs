using System;
using System.Collections;
using SP.Runtime.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Notification
{
    [RequireComponent(typeof(RectTransform), typeof(Animation))]
    public class NotificationElement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _image; 
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private LocalizeStringHelper _textLocalize;
        [SerializeField] private ContentSizeFitter _textContentSizeFitter;

        [Header("Settings")] 
        [SerializeField] private Color _neutralColor;
        [SerializeField] private Color _neutralTextColor;
        [SerializeField] private Color _warningColor;
        [SerializeField] private Color _warningTextColor;
        [SerializeField] private Color _infoColor;
        [SerializeField] private Color _infoTextColor;
        
        [Space(10)]
        
        [SerializeField] private float _fadeTime = 1.35f;
        
        public string Text
        {
            set
            {
                _textLocalize.SetEntry(value);
                UpdateRect();
            }
        }

        private NotificationType? _notificationType;
        public NotificationType? NotificationType
        {
            get => _notificationType;
            set
            {
                if (_notificationType != value)
                {
                    _notificationType = value;
                    UpdateNotificationType();
                }
            }
        }
        
        private bool _isFixed;
        public bool IsFixed => _isFixed;

        private const float _verticalPadding = 5;
        
        private const string _warningAnimation = "Warning";

        private RectTransform _rectTransform;
        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
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

        public void Initialize(string text, NotificationType notificationType, bool toFix)
        {
            if (!toFix)
            {
                StartCoroutine(Destroy());
            }

            Text = text;
            
            NotificationType = notificationType;
            
            _isFixed = toFix;
        }

        private void UpdateNotificationType()
        {
            switch (NotificationType)
            {
                case Notification.NotificationType.Neutral:
                {
                    _image.color = _neutralColor;
                    _text.color = _neutralTextColor;
                    break;
                }
                case Notification.NotificationType.Warning:
                {
                    _image.color = _warningColor;
                    _text.color = _warningTextColor;
                    Animation.Play(_warningAnimation);
                    break;
                }
                case Notification.NotificationType.Info:
                {
                    _image.color = _infoColor;
                    _text.color = _infoTextColor;
                    break;
                }
                case null:
                    break;
            }
        }

        private IEnumerator Destroy()
        {
            yield return new WaitForSeconds(2.5f);

            _textContentSizeFitter.enabled = false;

            while (_canvasGroup.alpha > 0)
            {
                _canvasGroup.alpha = Mathf.Clamp01(_canvasGroup.alpha - _fadeTime * Time.deltaTime);
                
                yield return new WaitForEndOfFrame();
            }

            Destroy(gameObject);
        }

        private void UpdateRect()
        {
            Canvas.ForceUpdateCanvases();
            
            RectTransform.sizeDelta = new Vector2(
                RectTransform.sizeDelta.x, 
                _text.rectTransform.rect.height + _verticalPadding);
        }
    }
}
