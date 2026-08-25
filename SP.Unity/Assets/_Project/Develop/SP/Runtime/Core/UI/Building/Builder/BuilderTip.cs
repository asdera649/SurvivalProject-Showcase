using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace SP.Runtime.Core.UI.Building.Builder
{
    [RequireComponent(typeof(RectTransform))]
    public class BuilderTip : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private LocalizeStringEvent _textLocalize;
        [SerializeField] private TMP_Text _text;

        private const float _horizontalPadding = 20;
        
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

        public void SetText(string entry)
        {
            _textLocalize.SetEntry(entry);
            
            Canvas.ForceUpdateCanvases();
            
            RectTransform.sizeDelta = new Vector2(
                _text.rectTransform.rect.width + _horizontalPadding, 
                RectTransform.sizeDelta.y);
        }
    }
}