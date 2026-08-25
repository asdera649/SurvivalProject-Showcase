using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI
{
    public class SleepingBagRenameMenu : MonoBehaviour
    {
        public event UnityAction<string> RenameConfirmed;
        public event UnityAction<SleepingBagRenameMenu> Destroyed;
    
        [SerializeField] private TMP_InputField _renameInputField;

        [SerializeField] private Button _cancelButton;
        public Button CancelButton => _cancelButton;

        [SerializeField] private Button _confirmButton;

        private void Start()
        {
            _confirmButton.onClick.AddListener(OnConfirmButtonClick);
        }

        public void Initialize(string sleepingBagName)
        {
            _renameInputField.text = sleepingBagName;
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmButtonClick);
            Destroyed?.Invoke(this);
        }

        private void OnConfirmButtonClick()
        {
            RenameConfirmed?.Invoke(_renameInputField.text);
        }
    }
}
