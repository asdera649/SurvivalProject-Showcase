using SP.Runtime.Core.Items;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;

namespace SP.Runtime.Core.UI.Inventory
{
    [RequireComponent(typeof(Animation))]
    public class ItemInfoBlock : MonoBehaviour
    {
        public event UnityAction<BaseItem, BaseItem.IReadOnlyAction> ItemActionExecuted; 
        
        [Header("Prefabs")]
        [SerializeField] private ActionButton _actionButtonPrefab;

        [Header("Reference")] 
        [SerializeField] private GameObject _group;
        [SerializeField] private LocalizeStringEvent _nameLocalize;
        [SerializeField] private LocalizeStringEvent _descriptionLocalize;
        [SerializeField] private Transform _actionsBlock;

        private BaseItem _item;
        
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
        
        public void Initialize(BaseItem item)
        {
            _item = item;
            
            foreach (Transform t in _actionsBlock.transform)
            {
                Destroy(t.gameObject);
            }

            _group.SetActive(_item != null);

            if (_group.activeSelf)
            {
                Animation.Play();
            }

            if (_item == null)
            {
                return;
            }
            
            _nameLocalize.SetEntry(_item.Name);
            _descriptionLocalize.SetEntry(_item.Description);
            
            var actions = _item.GetReadOnlyActions();
                
            foreach (var a in actions)
            {
                var button = Instantiate(
                    _actionButtonPrefab.gameObject,
                    _actionsBlock.transform).GetComponent<ActionButton>();

                button.Clicked += OnActionButtonClicked;
                    
                button.Initialize(a);
            }
        }

        private void OnActionButtonClicked(ActionButton actionButton)
        {
            if (_item == null)
            {
                return;
            }
            
            ItemActionExecuted?.Invoke(_item, actionButton.Action);
        }
    }
}
