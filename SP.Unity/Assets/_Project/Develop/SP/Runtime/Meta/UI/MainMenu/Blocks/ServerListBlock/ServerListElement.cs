using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    [RequireComponent(
        typeof(RectTransform),
        typeof(Button),
        typeof(Image))]
    public class ServerListElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event UnityAction<ServerListElement> FavoriteButtonClicked;
        public event UnityAction<ServerListElement> HoldCompleted;
        public event UnityAction<ServerListElement> Clicked;

        [Header("References")]
        [SerializeField] private TMP_Text _serverNameText;
        [SerializeField] private TMP_Text _playersText;
        [SerializeField] private TMP_Text _pingText;
        [SerializeField] private GameObject _tipText;
        [SerializeField] private Image _holdIndicator;
        [SerializeField] private Button _favoriteButton;
        [SerializeField] private Image _favoriteIcon;
        
        [Header("Settings")]
        [SerializeField] private Color _elementInactiveColor;
        [SerializeField] private Color _elementActiveColor;
        [SerializeField] private float _elementInactiveHeight = 45f;
        [SerializeField] private float _elementActiveHeight = 90f;
        [SerializeField] private Color _favoriteButtonInactiveColor;
        [SerializeField] private Color _favoriteButtonActiveColor;
        
        public string Name { get; private set; }
        public string UniqueId { get; private set; }

        private bool _isFavorite;
        private bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;

                _favoriteIcon.color = _isFavorite ? _favoriteButtonActiveColor : _favoriteButtonInactiveColor;
            }
        }
        
        private const float _fadeTime = 12f;
        private const float _holdTime = 1.3f;
        
        private bool _isActive;

        private bool _isHold;

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
        
        private Button _button;
        private Button Button
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
        
        private Image _image;
        private Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = GetComponent<Image>();
                }

                return _image;
            }
        }

        public void Initialize(
            string uniqueId,
            string serverName,
            int maxPlayersCount,
            int currentPlayersCount,
            long ping,
            bool isFavorite)
        {
            Name = serverName;
            UniqueId = uniqueId;
            _serverNameText.text = serverName;
            _playersText.text = currentPlayersCount + "/" + maxPlayersCount;
            _pingText.text = ping.ToString();
            IsFavorite = isFavorite;
        }

        private void OnEnable()
        {
            Button.onClick.AddListener(OnClicked);
            _favoriteButton.onClick.AddListener(OnFavoriteButtonClicked);
        }

        private void OnDisable()
        {
            Button.onClick.RemoveListener(OnClicked);
            _favoriteButton.onClick.RemoveListener(OnFavoriteButtonClicked);
        }

        private void Update()
        {
            UpdateHeight();
            
            UpdateHolding();
        }

        private void UpdateHeight()
        {
            RectTransform.sizeDelta = new Vector2(
                RectTransform.sizeDelta.x,
                Mathf.Lerp(
                    RectTransform.rect.height,
                    _isActive ? _elementActiveHeight : _elementInactiveHeight,
                    _fadeTime * Time.deltaTime));
        }

        private void UpdateHolding()
        {
            if (_isHold)
            {
                _holdIndicator.fillAmount += _holdTime * Time.deltaTime;
                
                if (_holdIndicator.fillAmount >= 1)
                {
                    HoldCompleted?.Invoke(this);
                    
                    _isHold = false;
                }
            }
            else
            {
                _holdIndicator.fillAmount = 0;
            }
        }

        public void SetActive(bool value)
        {
            _isActive = value;

            Image.color = value ? _elementActiveColor : _elementInactiveColor;
            
            _tipText.SetActive(value);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isActive)
            {
                _isHold = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isActive)
            {
                _isHold = false;
            }
        }
        
        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }

        private void OnFavoriteButtonClicked()
        {
            IsFavorite = !IsFavorite;
            
            FavoriteButtonClicked?.Invoke(this);
        }
    }
}
