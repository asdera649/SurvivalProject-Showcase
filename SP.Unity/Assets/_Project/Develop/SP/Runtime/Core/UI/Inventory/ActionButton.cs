using SP.Runtime.Core.Items;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory
{
    [RequireComponent(typeof(Button))]
    public class ActionButton : MonoBehaviour
    {
        public event UnityAction<ActionButton> Clicked; 
        
        [Header("References")]
        [SerializeField] private LocalizeStringEvent _nameLocalize;
        
        private BaseItem.IReadOnlyAction _action;
        public BaseItem.IReadOnlyAction Action => _action;

        private Button _button;
        private Button Button
        {
            get
            {
                if (_button == null)
                {
                    _button = GetComponent<Button>();
                }

                return _button;
            }
        }

        private void OnEnable()
        {
            Button.onClick.AddListener(OnClicked);
        }

        public void Initialize(BaseItem.IReadOnlyAction action)
        {
            _action = action;
            _nameLocalize.SetEntry(action.Name);
        }

        private void OnDisable()
        {
            Button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
