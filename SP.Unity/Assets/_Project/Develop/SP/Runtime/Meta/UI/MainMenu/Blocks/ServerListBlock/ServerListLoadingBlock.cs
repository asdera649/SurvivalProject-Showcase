using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    public class ServerListLoadingBlock : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Image _loadingIcon;

        [Header("Settings")] 
        [SerializeField] private float _loadingIconRotateSpeed = 250;

        private void Update()
        {
            _loadingIcon.rectTransform.Rotate(0, 0, -_loadingIconRotateSpeed * Time.deltaTime);
        }
    }
}