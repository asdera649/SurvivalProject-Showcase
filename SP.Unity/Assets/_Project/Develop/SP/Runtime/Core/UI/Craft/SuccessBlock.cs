using SP.Runtime.Localization;
using UnityEngine;

namespace SP.Runtime.Core.UI.Craft
{
    [RequireComponent(typeof(Animation))]
    public class SuccessBlock : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LocalizeStringHelper _successTextLocalize;

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

        private void OnDisable()
        {
            _successTextLocalize.gameObject.SetActive(false);
        }

        public void ShowSuccessText(string entry)
        {
            _successTextLocalize.gameObject.SetActive(true);
            
            _successTextLocalize.SetEntry(entry);

            if (Animation.isPlaying)
            {
                Animation.Stop();
            }
            
            Animation.Play();
        }
    }
}