using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.UIControls.RadialMenu
{
    [RequireComponent(typeof(Image))]
    public class RadialMenuButton : MonoBehaviour
    {
        public event UnityAction<RadialMenuButton> DataUpdateAction; 
        public event UnityAction<RadialMenuButton> ExecuteAction;
        public event UnityAction<RadialMenuButton> DestroyAction;

        private string _nameEntry;
        public string NameEntry
        {
            get => _nameEntry;
            set
            {
                _nameEntry = value;
                DataUpdateAction?.Invoke(this);
            }
        }
    
        private string _descriptionEntry;
        public string DescriptionEntry
        {
            get => _descriptionEntry;
            set
            {
                _descriptionEntry = value;
                DataUpdateAction?.Invoke(this);
            }
        }

        public bool Interactable { get; set; } = true;

        private Image _imageComponent;
        public Image ImageComponent
        {
            get
            {
                if (_imageComponent == null)
                {
                    _imageComponent = GetComponent<Image>();
                }
                
                return _imageComponent;
            }
        }

        private void OnDestroy()
        {
            DestroyAction?.Invoke(this);
        }

        public void Execute()
        {
            ExecuteAction?.Invoke(this);
        }
    }
}