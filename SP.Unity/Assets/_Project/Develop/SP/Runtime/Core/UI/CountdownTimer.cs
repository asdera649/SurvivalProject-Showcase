using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI
{
    [RequireComponent(typeof(Image), typeof(CanvasGroup))]
    public class CountdownTimer : MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent _actionNameLocalize;
        [SerializeField] private TMP_Text _timeText;

        private Coroutine _timer;

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
        
        private Image _image;
        private Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = GetComponent<Image>();
                }

                return _image;
            }
        }

        private void Start()
        {
            SetView(false);
        }

        private void SetView(bool value)
        {
            CanvasGroup.alpha = value ? 1 : 0;
            CanvasGroup.interactable = value;
            CanvasGroup.blocksRaycasts = value;
        }

        public void StartTimer(string actionNameEntry, float time)
        {
            if (_timer != null)
            {
                return;
            }

            _actionNameLocalize.SetEntry(actionNameEntry);
            
            _timer = StartCoroutine(Timer(time));
        }

        private IEnumerator Timer(float time)
        {
            SetView(true);
            
            var currentTime = time;
            
            while (currentTime > 0)
            {
                Image.fillAmount = currentTime / time;
                _timeText.text = currentTime.ToString("F1");
                
                currentTime -= Time.deltaTime;
                
                yield return new WaitForEndOfFrame();
            }

            SetView(false);
            
            _timer = null;
        }
    }
}
