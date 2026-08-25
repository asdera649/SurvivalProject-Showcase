using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SP.Runtime.Loading.UI.LoadingMenu
{
    public class LoadingMenu : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private CanvasGroup _mainGroup;
        [SerializeField] private Image _loadingIcon;
        [FormerlySerializedAs("_failBlock")] [SerializeField] private LoadingMenuFailBlock _loadingMenuFailBlock;

        [Header("Settings")] 
        [SerializeField] private float _loadingIconRotateSpeed = 500;

        private void Update()
        {
            UpdateLoadingIcon();
        }
        
        private void UpdateLoadingIcon()
        {
            _loadingIcon.rectTransform.Rotate(0, 0, -_loadingIconRotateSpeed * Time.deltaTime);
        }

        public void ShowFailBlock(LoadingMenuFailBlock.FailType failType)
        {
            _mainGroup.alpha = 0.2f;
            _loadingMenuFailBlock.Show(failType);
        }
    }
}