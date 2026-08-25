using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.MiniMap
{
    [RequireComponent(typeof(Image), typeof(Animation))]
    public class PlacemarkInfoBlock : MonoBehaviour
    {
        public event UnityAction<Placemark> DeleteAction;

        [Header("References")] 
        [SerializeField] private TMP_Text _placemarkText;
        [SerializeField] private TMP_Text _placemarkName;
        [SerializeField] private Button _deleteButton;

        private Placemark _placemark;

        private Image _image;
        public Image Image 
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
        
        public void Initialize(Placemark placemark)
        {
            _placemark = placemark;
            _placemarkName.text = placemark.Name;
            _deleteButton.interactable = !placemark.DontDelete;

            _placemarkText.gameObject.SetActive(string.IsNullOrEmpty(placemark.Name));
            _placemarkName.gameObject.SetActive(!string.IsNullOrEmpty(placemark.Name));
        }
        
        private void OnEnable()
        {
            _deleteButton.onClick.AddListener(OnDeleteButtonClick);
        }

        private void OnDisable()
        {
            _deleteButton.onClick.RemoveListener(OnDeleteButtonClick);
        }

        private void OnDeleteButtonClick()
        {
            if (_placemark == null)
            {
                return;
            }

            DeleteAction?.Invoke(_placemark);
        }
    }
}
