using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI
{
    public class ServerLoadingMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider _progressBar;

        private float _progress;
        public float Progress
        {
            set
            {
                _progress = Mathf.Clamp01(value);
                _progressBar.value = _progress;
            } 
            
        }
    }
}
