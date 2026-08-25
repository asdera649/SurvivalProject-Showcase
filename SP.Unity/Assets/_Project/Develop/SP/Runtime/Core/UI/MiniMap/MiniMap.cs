using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using SP.Runtime.Core.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.MiniMap
{
    [RequireComponent(typeof(CanvasGroup), typeof(Animation))]
    public class MiniMap : MonoBehaviour
    {
        #region Structs
        
        public class PlacemarkContainer
        {
            public PlacemarkContainer(
                Placemark placemark,
                GameObject target,
                Vector3 worldPosition,
                string name,
                string description,
                Sprite icon,
                Color? color,
                bool clampOnMiniMap,
                bool dontSave,
                bool dontDelete
            )
            {
                Placemark = placemark;
                Target = target;
                WorldPosition = worldPosition;
                Name = name;
                Description = description;
                Icon = icon;
                Color = color;
                ClampOnMiniMap = clampOnMiniMap;
                DontSave = dontSave;
                DontDelete = dontDelete;
            }
            
            public Placemark Placemark { get; }
            public GameObject Target { get; }
            public Vector3 WorldPosition { get; }
            public string Name { get; }
            public string Description { get; }
            public Sprite Icon { get; }
            public Color? Color { get; }
            public bool ClampOnMiniMap { get; }
            public bool DontSave { get; }
            public bool DontDelete { get; }
        }
        
        #endregion

        public event UnityAction<PlacemarkContainer> PlacemarkAdded; 
        public event UnityAction<PlacemarkContainer> PlacemarkRemoved; 
        
        [Header("Prefabs")]
        [SerializeField] private Placemark _placemarkPrefab;
        [SerializeField] private Image _targetPrefab;
    
        [Header("Reference")]
        [SerializeField] private Button _miniMap;
        [SerializeField] private GameObject _solidBackground;
        [SerializeField] private Map _map;
        [SerializeField] private AddingPlacemarkBlock _addingPlacemarkBlock;
        [SerializeField] private PlacemarkInfoBlock _placemarkInfoBlock;
        [SerializeField] private Button _backButton;

        [SerializeField] private StudioEventEmitter _clickEventEmitter;

        [Header("Settings")]
        [SerializeField] private float _worldDeltaSize = 911;

        [Space(10)] 
        
        [SerializeField] private Vector2 _fullMapStartPositionOffset;
        [SerializeField] private Vector3 _fullMapRotationOffset;
        
        [Space(10)]
        
        [SerializeField] private float _boundX = 512;
        [SerializeField] private float _boundY = 512;
        
        [Space(10)]
        
        [SerializeField] private float _defaultZoom = 1;
        [SerializeField] private float _maxZoom = 2;
        [SerializeField] private float _minZoom = 0.75f;
        
        [Space(10)]
        
        [SerializeField] private float _miniMapZoom = 1;
        
        private Transform _target;
        public Transform Target
        {
            get => _target;
            set
            {
                var oldValue = _target;
                _target = value;
                
                OnTargetUpdate(oldValue, _target);
            }
        }
        
        private bool _isFullMapOpen;
        private bool IsFullMapOpen
        {
            get => _isFullMapOpen;
            set
            {
                var oldValue = _isFullMapOpen;
                _isFullMapOpen = value;
                
                OnFullMapUpdate(oldValue, _isFullMapOpen);
            }
        }

        private static readonly List<PlacemarkObject> _startPlacemarks = new();
        
        private readonly List<PlacemarkContainer> _placemarks = new();
        
        private Image _targetIcon;
        
        private Vector2 _mapLastPosition;

        private RectTransform _mapRectTransform;

        private Coroutine _settingViewViaDelay;

        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }

                return _canvasGroup;
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

        private void Awake()
        {
            _mapRectTransform = _map.Image.rectTransform;
            _mapLastPosition = _mapRectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            _miniMap.onClick.AddListener(OnMiniMapClick);
            _backButton.onClick.AddListener(OnMiniMapClick);
            _map.MapInput.PointerUpAction += OnMapPointerUp;
            _map.MapInput.SwipeAction += OnMapSwipe;
            _map.MapInput.PinchAction += OnMapPinch;
            _addingPlacemarkBlock.PlaceAction += OnPlacePlacemark;
            _placemarkInfoBlock.DeleteAction += OnDeletePlacemark;
        }

        private void Start()
        {
            foreach (var p in _startPlacemarks)
            {
                AddPlacemark(
                    p.transform.position,
                    p.PlacemarkName,
                    p.PlacemarkDescription,
                    p.PlacemarkIcon,
                    true,
                    false,
                    true
                );
            }

            IsFullMapOpen = IsFullMapOpen;
        }

        private void OnDisable()
        {
            _miniMap.onClick.RemoveListener(OnMiniMapClick);
            _backButton.onClick.RemoveListener(OnMiniMapClick);
            _map.MapInput.PointerUpAction -= OnMapPointerUp;
            _map.MapInput.SwipeAction -= OnMapSwipe;
            _map.MapInput.PinchAction -= OnMapPinch;
            _addingPlacemarkBlock.PlaceAction -= OnPlacePlacemark;
            _placemarkInfoBlock.DeleteAction -= OnDeletePlacemark;
        }

        public void Initialize(
            Sprite mapTexture,
            float worldDeltaSize,
            Vector3 fullMapStartPositionOffset,
            Vector3 fullMapRotationOffset,
            float boundX,
            float boundY,
            float defaultZoom,
            float maxZoom,
            float minZoom,
            float miniMapZoom)
        {
            _map.Image.sprite = mapTexture;
            _worldDeltaSize = worldDeltaSize;
            _fullMapStartPositionOffset = fullMapStartPositionOffset;
            _fullMapRotationOffset = fullMapRotationOffset;
            _boundX = boundX;
            _boundY = boundY;
            _defaultZoom = defaultZoom;
            _maxZoom = maxZoom;
            _minZoom = minZoom;
            _miniMapZoom = miniMapZoom;
        }

        #region Update

        private void Update()
        {
            UpdateMapContents();
        }

        private void LateUpdate()
        {
            UpdateMapContentsScale();
        }
        
        private void UpdateMapContents()
        {
            if (Target != null && _targetIcon != null)
            {
                var xPercent = Target.position.x / _worldDeltaSize * 100;
                var yPercent = Target.position.z / _worldDeltaSize * 100;

                _targetIcon.rectTransform.anchoredPosition = new Vector2(
                    _mapRectTransform.sizeDelta.x / 100 * xPercent,
                    _mapRectTransform.sizeDelta.y / 100 * yPercent);
                
                _targetIcon.rectTransform.eulerAngles = new Vector3(
                    _targetIcon.rectTransform.eulerAngles.x,
                    _targetIcon.rectTransform.eulerAngles.y,
                    _mapRectTransform.eulerAngles.z - _target.eulerAngles.y);

                if (!IsFullMapOpen)
                {
                    var targetIconOffsetPosition = new Vector2(
                        _mapRectTransform.sizeDelta.x / 2 + _targetIcon.rectTransform.anchoredPosition.x,
                        _mapRectTransform.sizeDelta.y / 2 + _targetIcon.rectTransform.anchoredPosition.y);

                    _mapRectTransform.pivot = new Vector2(
                        targetIconOffsetPosition.x / _mapRectTransform.sizeDelta.x,
                        targetIconOffsetPosition.y / _mapRectTransform.sizeDelta.y);
                    
                    _mapRectTransform.eulerAngles = new Vector3(
                        _mapRectTransform.eulerAngles.x,
                        _mapRectTransform.eulerAngles.y,
                        Loader.Instance.MainCamera.Camera.transform.eulerAngles.y);
                }
            }

            foreach (var p in _placemarks)
            {
                Vector2 percent;

                if (p.Target != null)
                {
                    percent = new Vector2(
                        p.Target.transform.position.x / _worldDeltaSize * 100,
                        p.Target.transform.position.z / _worldDeltaSize * 100);
                }
                else
                {
                    percent = new Vector2(
                        p.WorldPosition.x / _worldDeltaSize * 100,
                        p.WorldPosition.z / _worldDeltaSize * 100);
                }
                
                var newPosition = new Vector2(
                    _mapRectTransform.sizeDelta.x / 100 * percent.x,
                    _mapRectTransform.sizeDelta.y / 100 * percent.y);

                p.Placemark.Image.rectTransform.anchoredPosition = newPosition;
                
                if (!IsFullMapOpen && p.ClampOnMiniMap)
                {
                    var pivotPosition = new Vector2(
                                            _mapRectTransform.rect.width * _mapRectTransform.pivot.x,
                                            _mapRectTransform.rect.height * _mapRectTransform.pivot.y) - 
                                        new Vector2(
                                            _mapRectTransform.rect.width / 2,
                                            _mapRectTransform.rect.height / 2);

                    p.Placemark.Image.rectTransform.localPosition = Vector2.ClampMagnitude(
                        newPosition - pivotPosition,
                        _miniMap.image.rectTransform.rect.width / 2 / _miniMapZoom);
                }

                p.Placemark.Image.rectTransform.rotation = Quaternion.Euler(_map.transform.forward);
            }
        }

        private void UpdateMapContentsScale()
        {
            var scale = new Vector3(
                1 / _map.transform.localScale.x,
                1 / _map.transform.localScale.y,
                1 / _map.transform.localScale.z);

            if (_addingPlacemarkBlock.gameObject.activeInHierarchy)
            {
                _addingPlacemarkBlock.transform.localScale = scale;
            }

            if (_placemarkInfoBlock.gameObject.activeInHierarchy)
            {
                _placemarkInfoBlock.transform.localScale = scale;
            }
        }
        
        #endregion

        public void SetView(bool value)
        {
            if (_settingViewViaDelay != null)
            {
                StopCoroutine(_settingViewViaDelay);
                _settingViewViaDelay = null;
            }
            
            CanvasGroup.alpha = value ? 1 : 0;
            CanvasGroup.interactable = value;
            CanvasGroup.blocksRaycasts = value;
        }
        
        public void SetView(bool value, float delay)
        {
            if (_settingViewViaDelay != null)
            {
                StopCoroutine(_settingViewViaDelay);
                _settingViewViaDelay = null;
            }
            
            _settingViewViaDelay = StartCoroutine(SetViewViaDelay(value, delay));
        }

        private IEnumerator SetViewViaDelay(bool value, float delay)
        {
            yield return new WaitForSeconds(delay);

            SetView(value);

            _settingViewViaDelay = null;
            
            yield return null;
        }

        public static void AddStartPlacemark(PlacemarkObject placemarkObject)
        {
            if (_startPlacemarks.Contains(placemarkObject))
            {
                return;
            }
            
            _startPlacemarks.Add(placemarkObject);
        }

        public static void RemoveStartPlacemark(PlacemarkObject placemarkObject)
        {
            if (!_startPlacemarks.Contains(placemarkObject))
            {
                return;
            }
            
            _startPlacemarks.Remove(placemarkObject);
        }

        public PlacemarkContainer AddPlacemark(
            Vector3 worldPosition,
            string placemarkName,
            string placemarkDescription,
            Sprite icon,
            bool dontDelete,
            bool clampOnMiniMap, 
            bool dontSave,
            Color? color = null)
        {
            var placemark = Instantiate(_placemarkPrefab, _map.transform);
            
            placemark.Mask.MaskArea = _miniMap.image.rectTransform;
            
            placemark.Initialize(placemarkName, icon, color, placemarkDescription, dontDelete);

            placemark.ClickAction += OnPlacemarkClick;

            var placemarkContainer = new PlacemarkContainer(
                placemark,
                null,
                worldPosition,
                placemarkName,
                placemarkDescription,
                icon,
                color,
                clampOnMiniMap,
                dontSave,
                dontDelete);
            
            _placemarks.Add(placemarkContainer);
            
            PlacemarkAdded?.Invoke(placemarkContainer);

            UpdatePlacemarksMask();

            if (_targetIcon != null)
            {
                _targetIcon.transform.SetAsLastSibling();
            }
            
            _placemarkInfoBlock.transform.SetAsLastSibling();
            _addingPlacemarkBlock.transform.SetAsLastSibling();

            return placemarkContainer;
        }

        public void RemovePlacemark(PlacemarkContainer placemark)
        {
            if (_placemarks.Contains(placemark))
            {
                placemark.Placemark.ClickAction -= OnPlacemarkClick;

                Destroy(placemark.Placemark.gameObject);

                _placemarks.Remove(placemark);

                PlacemarkRemoved?.Invoke(placemark);
            }
        }
        
        private void RemovePlacemark(Placemark placemark)
        {
            var index = _placemarks.FindIndex(p => p.Placemark == placemark);

            if (index == -1)
            {
                return;
            }
            
            RemovePlacemark(_placemarks[index]);
        }
        
        private void UpdatePlacemarksMask()
        {
            foreach (var p in _placemarks)
            {
                p.Placemark.Image.raycastTarget = IsFullMapOpen;

                p.Placemark.Mask.SetActiveMask(!((p.ClampOnMiniMap && !IsFullMapOpen) || IsFullMapOpen));
            }
        }

        private void SetActiveAddingPlacemarkBlock(bool value)
        {
            _addingPlacemarkBlock.gameObject.SetActive(value);

            if (value)
            {
                _clickEventEmitter.Play();
                _addingPlacemarkBlock.Animation.Play();
            }
        }
        
        private void SetActivePlacemarkInfoBlock(bool value)
        {
            _placemarkInfoBlock.gameObject.SetActive(value);
            
            if (value)
            {
                _clickEventEmitter.Play();
                _placemarkInfoBlock.Animation.Play();
            }
        }

        #region Callbacks

        #region Control
        
        private void OnMiniMapClick()
        {
            IsFullMapOpen = !IsFullMapOpen;
        }
        
        private void OnMapPointerUp(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRectTransform,
                position,
                null,
                out var localPoint);
                
            _addingPlacemarkBlock.RectTransform.anchorMin = _mapRectTransform.pivot;
            _addingPlacemarkBlock.RectTransform.anchorMax = _mapRectTransform.pivot;
            _addingPlacemarkBlock.RectTransform.anchoredPosition = localPoint;
                
            SetActivePlacemarkInfoBlock(false);
            SetActiveAddingPlacemarkBlock(true);
        }

        private void OnMapSwipe(Vector2 deltaPosition)
        {
            OnSwipe(deltaPosition);
            
            SetActiveAddingPlacemarkBlock(false);
            SetActivePlacemarkInfoBlock(false);
        }

        private void OnMapPinch(Vector2 center, float delta)
        {
            OnPinch(center, delta);
            
            SetActiveAddingPlacemarkBlock(false);
            SetActivePlacemarkInfoBlock(false);
        }
        
        private void OnSwipe(Vector2 deltaPosition)
        {
            var newPos = _mapRectTransform.anchoredPosition + deltaPosition;
            var currentScale = _mapRectTransform.localScale.x;
            
            newPos.x = Mathf.Clamp(newPos.x, -_boundX * currentScale, _boundX * currentScale);
            newPos.y = Mathf.Clamp(newPos.y, -_boundY * currentScale, _boundY * currentScale);

            _mapRectTransform.anchoredPosition = newPos;
        }

        private void OnPinch(Vector2 center, float delta)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRectTransform.parent as RectTransform,
                center,
                null,
                out var pinchLocalPos);

            var oldScale = _mapRectTransform.localScale.x;
            var newScale = Mathf.Clamp(oldScale + delta, _minZoom, _maxZoom);
            var scaleFactor = newScale / oldScale;

            var oldPos = _mapRectTransform.anchoredPosition;
            var newPos = pinchLocalPos + (oldPos - pinchLocalPos) * scaleFactor;
            
            _mapRectTransform.localScale = Vector3.one * newScale;
            
            newPos.x = Mathf.Clamp(newPos.x, -_boundX * newScale, _boundX * newScale);
            newPos.y = Mathf.Clamp(newPos.y, -_boundY * newScale, _boundY * newScale);
            
            _mapRectTransform.anchoredPosition = newPos;
        }
        
        #endregion
        
        private void OnPlacemarkClick(Placemark placemark)
        {
            if (!IsFullMapOpen)
            {
                return;
            }

            _placemarkInfoBlock.Image.rectTransform.anchoredPosition = placemark.Image.rectTransform.anchoredPosition;
            _placemarkInfoBlock.Initialize(placemark);
            SetActivePlacemarkInfoBlock(true);
            SetActiveAddingPlacemarkBlock(false);
        }
        
        private void OnPlacePlacemark(string placemarkName, Sprite icon, Color color)
        {
            var position = 
                _addingPlacemarkBlock.RectTransform.anchorMin * 
                _mapRectTransform.rect.width +
                _addingPlacemarkBlock.RectTransform.anchoredPosition;
        
            var percent = new Vector2(
                position.x / _mapRectTransform.rect.width,
                position.y / _mapRectTransform.rect.height);
            
            var worldPosition = new Vector3(_worldDeltaSize * percent.x, 0, _worldDeltaSize * percent.y);
            
            worldPosition -= new Vector3(_worldDeltaSize, 0, _worldDeltaSize) / 2;

            AddPlacemark(worldPosition, placemarkName, null, icon, false, true, false, color);

            SetActiveAddingPlacemarkBlock(false);
        }

        private void OnDeletePlacemark(Placemark placemark)
        {
            RemovePlacemark(placemark);
            
            SetActivePlacemarkInfoBlock(false);
        }

        private void OnTargetUpdate(Transform oldValue, Transform newValue)
        {
            if (newValue == null && _targetIcon != null)
            {
                Destroy(_targetIcon.gameObject);
            }

            if (newValue != null && _targetIcon == null)
            {
                _targetIcon = Instantiate(_targetPrefab, _mapRectTransform);
                _targetIcon.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void OnFullMapUpdate(bool oldValue, bool newValue)
        {
            _miniMap.gameObject.SetActive(!IsFullMapOpen);
            _solidBackground.SetActive(IsFullMapOpen);
            _backButton.gameObject.SetActive(IsFullMapOpen);
            
            _map.Image.raycastTarget = IsFullMapOpen;
            _map.Mask.SetActiveMask(!IsFullMapOpen);
            _map.MapInput.enabled = IsFullMapOpen;

            if (IsFullMapOpen)
            {
                _mapRectTransform.anchorMin = Vector2.one / 2;
                _mapRectTransform.anchorMax = Vector2.one / 2;
                _mapRectTransform.pivot = Vector2.one / 2;
                _mapLastPosition = _mapRectTransform.anchoredPosition;
                _mapRectTransform.anchoredPosition = Vector2.zero + _fullMapStartPositionOffset;
                _mapRectTransform.eulerAngles = Vector3.zero + _fullMapRotationOffset;
                
                _addingPlacemarkBlock.RectTransform.localEulerAngles = Vector3.zero - _fullMapRotationOffset;
                _placemarkInfoBlock.Image.rectTransform.localEulerAngles = Vector3.zero - _fullMapRotationOffset;
                
                _mapRectTransform.localScale = 
                    Math.Clamp(_defaultZoom, _minZoom, _maxZoom) *
                    Vector3.one;

                Animation.Play();
            }
            else
            {
                _mapRectTransform.anchorMin = Vector2.one;
                _mapRectTransform.anchorMax = Vector2.one;
                _mapRectTransform.anchoredPosition = _mapLastPosition;
                _mapRectTransform.localScale = Vector3.one * _miniMapZoom;
                SetActiveAddingPlacemarkBlock(IsFullMapOpen);
                SetActivePlacemarkInfoBlock(IsFullMapOpen);
            }

            UpdatePlacemarksMask();
        }
        
        #endregion
    }
}
