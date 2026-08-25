using System.Collections;
using System.Collections.Generic;
using SP.Runtime.Core.Services.AuthorizationService;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Respawn
{
    [RequireComponent(typeof(CanvasGroup), typeof(Animation))]
    public class RespawnMenu : MonoBehaviour
    {
        public event UnityAction DefaultRespawnAction; 
        public event UnityAction<string> RespawnAction;

        [Header("Prefabs")]
        [SerializeField] private RespawnButton _respawnButtonPrefab;
        [SerializeField] private Animation _dummyRespawnButton;
    
        [Header("References")]
        [SerializeField] private Button _defaultRespawnButton;
        [SerializeField] private Transform _respawnButtonsContainer;

        private readonly List<RespawnButton> _respawnButtonsCache = new();
        
        private List<AuthorizationService.RespawnPoint> _respawnPointsCache = new();

        private Coroutine _respawnButtonInstantiateProcess;
        
        private bool _isAnimated;
        
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

        private void OnEnable()
        {
            _defaultRespawnButton.onClick.AddListener(OnDefaultRespawnButtonClick);
        }

        private void OnDisable()
        {
            _defaultRespawnButton.onClick.RemoveListener(OnDefaultRespawnButtonClick);
        }

        private void OnDestroy()
        {
            DestroyRespawnButtons();
        }

        public void Initialize(IReadOnlyList<AuthorizationService.RespawnPoint> spawnPoints)
        {
            DestroyRespawnButtons();

            _respawnPointsCache = new List<AuthorizationService.RespawnPoint>(spawnPoints);

            if (_isAnimated)
            {
                InstantiateRespawnButtonsWithoutAnimation();
            }
        }

        public void SetView(bool value)
        {
            gameObject.SetActive(value);

            if (!value)
            {
                CanvasGroup.alpha = 0;
            }
            else
            {
                _isAnimated = false;
                Animation.Play();
            }
        }

        private void InstantiateRespawnButtonsWithoutAnimation()
        {
            if (_respawnButtonInstantiateProcess != null)
            {
                StopCoroutine(_respawnButtonInstantiateProcess);
                _respawnButtonInstantiateProcess = null;
            }
            
            _respawnButtonInstantiateProcess = StartCoroutine(InstantiateRespawnButtons(_respawnPointsCache));

            _isAnimated = true;
        }
        
        // Call from animation event.
        public void InstantiateRespawnButtonsWithAnimation()
        {
            if (_respawnButtonInstantiateProcess != null)
            {
                StopCoroutine(_respawnButtonInstantiateProcess);
                _respawnButtonInstantiateProcess = null;
            }
            
            _respawnButtonInstantiateProcess = StartCoroutine(
                InstantiateRespawnButtons(_respawnPointsCache, true));
            
            _isAnimated = true;
        }

        private IEnumerator InstantiateRespawnButtons(
            IEnumerable<AuthorizationService.RespawnPoint> spawnPoints,
            bool isAnimating = false,
            float delay = 0.2f)
        {
            yield return new WaitForEndOfFrame(); // Для плавной анимации, не убирать!
            
            var count = 2;
            
            foreach (var p in spawnPoints)
            {
                var respawnButton = Instantiate(
                    _respawnButtonPrefab.gameObject,
                    _respawnButtonsContainer.transform).GetComponent<RespawnButton>();

                respawnButton.ClickAction += OnRespawnButtonClick;
                respawnButton.Initialize(p);
                
                _respawnButtonsCache.Add(respawnButton);

                count--;

                if (!isAnimating)
                {
                    continue;
                }
                
                respawnButton.Animation.Play();
                yield return new WaitForSeconds(delay);
            }

            for (var i = 0; i < count; i++)
            {
                var dummy = Instantiate(_dummyRespawnButton, _respawnButtonsContainer.transform);

                if (!isAnimating)
                {
                    continue;
                }
                
                dummy.Play();
                yield return new WaitForSeconds(delay);
            }
            
            _respawnButtonInstantiateProcess = null;

            yield return null;
        }

        public void DestroyRespawnButtons()
        {
            for (var i = _respawnButtonsCache.Count - 1; i >= 0; i--)
            {
                _respawnButtonsCache[i].ClickAction -= OnRespawnButtonClick;
                
                Destroy(_respawnButtonsCache[i].gameObject);
                
                _respawnButtonsCache.RemoveAt(i);
            }

            foreach (Transform c in _respawnButtonsContainer.transform)
            {
                Destroy(c.gameObject);
            }
        }

        #region Callbacks

        private void OnDefaultRespawnButtonClick()
        {
            DefaultRespawnAction?.Invoke();
        }

        private void OnRespawnButtonClick(string uniqueId)
        {
            RespawnAction?.Invoke(uniqueId);
        }
        
        #endregion
    }
}
