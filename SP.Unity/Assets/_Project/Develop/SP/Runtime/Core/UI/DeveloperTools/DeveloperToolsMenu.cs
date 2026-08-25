using SP.Runtime.Core.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.DeveloperTools
{
    public class DeveloperToolsMenu : MonoBehaviour
    {
        public event UnityAction<float> TimeIsSet;
        public event UnityAction Saved;
        public event UnityAction<int> ItemClicked;
        
        [Header("Prefabs")]
        [SerializeField] private Item _itemPrefab;
        
        [Header("References")]
        [SerializeField] private GameObject _group;
        [SerializeField] private Button _turnButton;
        [SerializeField] private TMP_InputField _timeInputField;
        [SerializeField] private Button _setTimeButton;
        [SerializeField] private Button _saveButton;

        [SerializeField] private Transform _itemsContainer;
        
        public void Initialize(BaseItem[] receivedItems)
        {
            _turnButton.onClick.AddListener(OnTurnButtonClick);
            _setTimeButton.onClick.AddListener(OnSetTimeButtonClick);
            _saveButton.onClick.AddListener(OnSaveButtonClick);

            for (var i = 0; i < receivedItems.Length; i++)
            {
                var item = Instantiate(_itemPrefab, _itemsContainer);

                item.gameObject.name = i.ToString();
                
                item.Initialize(receivedItems[i].Icon);
                
                item.Clicked += OnItemClicked;
            }
        }

        private void OnDestroy()
        {
            _turnButton.onClick.RemoveListener(OnTurnButtonClick);
            _setTimeButton.onClick.RemoveListener(OnSetTimeButtonClick);
            _saveButton.onClick.RemoveListener(OnSaveButtonClick);
        }

        private void OnTurnButtonClick()
        {
            _group.SetActive(!_group.activeInHierarchy);
        }
    
        private void OnSetTimeButtonClick()
        {
            if (string.IsNullOrEmpty(_timeInputField.text))
            {
                return;
            }
            
            TimeIsSet?.Invoke(float.Parse(_timeInputField.text));
        }
    
        private void OnSaveButtonClick()
        {
            Saved?.Invoke();
        }
        
        private void OnItemClicked(Item item)
        {
            ItemClicked?.Invoke(int.Parse(item.name));
        }
    }
}
