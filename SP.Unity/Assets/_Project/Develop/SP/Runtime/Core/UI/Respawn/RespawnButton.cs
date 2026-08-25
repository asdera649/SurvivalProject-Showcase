using SP.Runtime.Core.Services.AuthorizationService;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Respawn
{
    [RequireComponent(typeof(Animation))]
    public class RespawnButton : MonoBehaviour
    {
        public event UnityAction<string> ClickAction;

        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private GameObject _cooldownBlock;
        [SerializeField] private TMP_Text _cooldownText;

        private AuthorizationService.RespawnPoint _respawnPoint;

        private const float _updateRate = 1;
        
        private Animation _animation;
        public Animation Animation
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
            _button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            InvokeRepeating(nameof(UpdateCooldown), _updateRate, _updateRate);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(UpdateCooldown));
        }

        public void Initialize(AuthorizationService.RespawnPoint respawnPoint)
        {
            _respawnPoint = respawnPoint;
            
            _text.text = _respawnPoint.Name;
            
            UpdateView();
        }

        private void UpdateCooldown()
        {
            _respawnPoint.UpdateCooldown();
            
            UpdateView();
        }

        private void UpdateView()
        {
            _cooldownText.text = Mathf.Ceil(_respawnPoint.Cooldown).ToString("0#");
            _cooldownBlock.SetActive(_respawnPoint.Cooldown > 0);
            
            _button.interactable = !(_respawnPoint.Cooldown > 0);
        }

        #region Callbacks

        private void OnClick()
        {
            ClickAction?.Invoke(_respawnPoint.UniqueId);
        }
        
        #endregion
    }
}
