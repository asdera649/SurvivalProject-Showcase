using System.Collections;
using System.Collections.Generic;
using SP.Runtime.Core.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class Slot : MonoBehaviour, IPointerEnterHandler
    {
        #region Structs
        
        public enum ProcessType
        {
            Move,
            Drop
        }
        
        #endregion
        
        public event UnityAction<Slot, ProcessType> ProcessExecuted;
        public event UnityAction<Slot> Clicked;

        [Header("Reference")] 
        [SerializeField] private GameObject _group;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _quantityText;
        
        [FormerlySerializedAs("_strenghtSlider")] 
        [SerializeField] private Slider _strengthSlider;
        
        [FormerlySerializedAs("_strenghtSliderImage")] 
        [SerializeField] private Image _strengthSliderImage;
        
        [SerializeField] private Image _moveProcessIcon;
        [SerializeField] private Image _dropProcessIcon;
        
        [Header("Settings")]
        [FormerlySerializedAs("_strenghtSliderNormalColor")]
        [SerializeField] private Color _strengthSliderNormalColor = Color.white;
        
        [FormerlySerializedAs("_strenghtSliderEndColor")] 
        [SerializeField] private Color _strengthSliderEndColor = Color.red;
        [SerializeField] private float _endThresholdValue = 0.3f;
        
        private BaseItem _item;
        public BaseItem Item => _item;

        private Systems.Inventory.Inventory _owner;
        public Systems.Inventory.Inventory Owner => _owner;

        private int _index;
        public int Index => _index;

        private Coroutine _process;
        
        private Color _backupColor;
        
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
        
        private void Awake()
        {
            _backupColor = Image.color;
        }

        private void OnEnable()
        {
            Button.onClick.AddListener(OnClicked);
        }

        private void Initialize(BaseItem item, Systems.Inventory.Inventory owner, int index)
        {
            _item = item;
            _owner = owner;
            _index = index;

            _group.SetActive(_item != null);

            if (_process == null)
            {
                _moveProcessIcon.gameObject.SetActive(false);
                _dropProcessIcon.gameObject.SetActive(false);
            }

            if (_item == null)
            {
                return;
            }

            _icon.sprite = _item.Icon;
            _quantityText.text = _item.GetQuantityText;
            
            _strengthSlider.gameObject.SetActive(_item.ConsiderStrength);

            if (_item.ConsiderStrength)
            {
                UpdateStrengthSlider();
            }
        }

        private void OnDisable()
        {
            Button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            if (_item == null)
            {
                return;
            }
            
            _quantityText.text = _item.GetQuantityText;
            
            UpdateStrengthSlider();
        }
        
        private void UpdateStrengthSlider()
        {
            _strengthSlider.value = _item.StockStrength;
            _strengthSliderImage.color = 
                _strengthSlider.value <= _endThresholdValue ?
                    _strengthSliderEndColor :
                    _strengthSliderNormalColor;
        }

        public void SetColor(Color color)
        {
            Image.color = color;
        }

        public void ResetColor()
        {
            Image.color = _backupColor;
        }

        public void StartMoveProcess()
        {
            if (_process != null || _item == null)
            {
                return;
            }

            _process = StartCoroutine(Process(ProcessType.Move));
        }

        public void StartDropProcess()
        {
            if (_process != null || _item == null)
            {
                return;
            }

            _process = StartCoroutine(Process(ProcessType.Drop));
        }

        #region Callbacks

        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (InventoryBackground.IsDoublePointerDown)
            {
                StartDropProcess();
            }
            else if (InventoryBackground.IsPointerDown)
            {
                StartMoveProcess();
            }
        }

        private IEnumerator Process(ProcessType process)
        {
            var target = _moveProcessIcon;

            switch (process)
            {
                case ProcessType.Move:
                {
                    target = _moveProcessIcon;
                    break;
                }
                case ProcessType.Drop:
                {
                    target = _dropProcessIcon;
                    break;
                }
            }

            target.gameObject.SetActive(true);
            
            while (target.fillAmount > 0)
            {
                target.fillAmount -= Time.deltaTime * 2.5f;
                yield return new WaitForEndOfFrame();
            }

            target.gameObject.SetActive(false);
            target.fillAmount = 1;
            
            ProcessExecuted?.Invoke(this, process);
            
            _process = null;
        }
        
        #endregion

        #region Statics
        
        public static IReadOnlyList<Slot> SpawnSlots(
            Slot slotPrefab,
            Systems.Inventory.Inventory owner,
            IReadOnlyList<Transform> parents,
            IReadOnlyList<BaseItem> items,
            IReadOnlyList<int> inventorySize)
        {
            List<Slot> outputSlots = new();

            var processedItems = -1;

            for (var p = 0; p < parents.Count; p++)
            {
                var addedSlots = 0;
                
                var slots = parents[p].GetComponentsInChildren<Slot>();
                
                if (p < inventorySize.Count)
                {
                    for (var i = 0; i < inventorySize[p]; i++)
                    {
                        processedItems++;

                        if (processedItems < items.Count)
                        {
                            Slot slot = null;

                            if (i < slots.Length)
                            {
                                slot = slots[i];
                            }

                            if (slot == null)
                            {
                                slot = Instantiate(slotPrefab.gameObject, parents[p]).GetComponent<Slot>();
                            }

                            slot.Initialize(items[processedItems], owner, processedItems);

                            outputSlots.Add(slot);
                            addedSlots++;
                        }
                        
                        if (i == inventorySize[p] - 1)
                        {
                            for (var s = slots.Length - 1; s > addedSlots - 1; s--)
                            {
                                Destroy(slots[s].gameObject);
                            }
                        }
                    }
                }
            }

            return outputSlots;
        }

        #endregion
    }
}