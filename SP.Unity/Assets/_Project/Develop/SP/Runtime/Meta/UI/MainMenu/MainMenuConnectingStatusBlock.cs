using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu
{
    public class MainMenuConnectingStatusBlock : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject _background;
        [SerializeField] private Image _loadingIcon;
        
        [Header("Settings")] 
        [SerializeField] private float _loadingIconRotateSpeed = 500;

        private void Update()
        {
            _loadingIcon.rectTransform.Rotate(0, 0, -_loadingIconRotateSpeed * Time.deltaTime);
        }
        
        public void Show()
        {
            _background.SetActive(true);
        }

        public void Hide()
        {
            _background.SetActive(false);
        }
    }
}