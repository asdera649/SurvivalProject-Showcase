using UnityEngine;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BaseBlock : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fadeTime = 17;

        private bool _isActive;
        
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

        private void Update()
        {
            UpdateCanvasGroupAlpha();
        }

        private void UpdateCanvasGroupAlpha()
        {
            CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, _isActive ? 1 : 0, _fadeTime * Time.deltaTime);
        }

        public void SetView(bool value)
        {
            _isActive = value;

            if (!_isActive)
            {
                CanvasGroup.alpha = 0;
            }
            
            CanvasGroup.interactable = value;
            CanvasGroup.blocksRaycasts = value;

            OnViewUpdate(value);
        }

        protected virtual void OnViewUpdate(bool value)
        { 
        
        }
    }
}
