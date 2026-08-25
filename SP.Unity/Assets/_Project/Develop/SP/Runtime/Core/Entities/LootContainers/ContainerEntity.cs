using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.MiningDetection;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using UnityEngine;

namespace SP.Runtime.Core.Entities.LootContainers
{
    [RequireComponent(typeof(MiningDetectionHandler))]
    public class ContainerEntity : LootableEntity<BaseAdditionalBlock>
    {
        #region Structs
        
        [System.Serializable]
        public class Content
        {
            [SerializeField] private BaseItem _item;
            public BaseItem Item => _item;

            [SerializeField] private int _minQuantity;
            public int MinQuantity => _minQuantity;

            [SerializeField] private int _maxQuantity;
            public int MaxQuantity => _maxQuantity;

            [SerializeField] [Range(0, 1)] private float _minStockStrength;
            public float MinStockStrength => _minStockStrength;

            [SerializeField] [Range(0, 1)] private float _maxStockStrength;
            public float MaxStockStrength => _maxStockStrength;
            
            [SerializeField] [Range(0, 1)] private float _probability;
            public float Probability => _probability;
        }

        [System.Serializable]
        public class ContentContainer
        {
            [SerializeField] private int _contentQuantity;
            public int ContentQuantity => _contentQuantity;

            [SerializeField] private Content[] _contentList;
            public IReadOnlyList<Content> ContentList => _contentList;
        }

        public class ResultContent
        {
            public ResultContent(BaseItem item, int quantity, float stockStrength)
            {
                Item = item;
                Quantity = quantity;
                StockStrength = stockStrength;
            }

            public BaseItem Item { get; }
            public int Quantity { get; }
            public float StockStrength { get; }
        }
            
        #endregion

        [Header("References")]
        [SerializeField] private Transform _containerLidTransform;
        
        [Header("Settings")]
        [SerializeField] private ContentContainer _content;
        [SerializeField] private float _restoreTime = 900;

        [SyncVar(hook = nameof(OnRestoringUpdated))] private bool _isRestoring;

        [SyncVar(hook = nameof(OnRemainingTimeToRestoreUpdated))] private float _remainingTimeToRestore;
        
        private Coroutine _restore;

        private MiningDetectionHandler _miningDetectionHandler;
        private MiningDetectionHandler MiningDetectionHandler
        {
            get
            {
                if (_miningDetectionHandler == null)
                {
                    _miningDetectionHandler = GetComponent<MiningDetectionHandler>();
                }

                return _miningDetectionHandler;
            }
        }
        
        private Vector3 _containerLidStartPosition;

        protected override void Awake()
        {
            base.Awake();
            
            _containerLidStartPosition = _containerLidTransform.localPosition;
        }
        
        public override void Start()
        {
            base.Start();

            if (!isServer)
            {
                return;
            }
            
            OnFill();
            
            Inventory.InventoryUpdated += OnInventoryUpdate;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Inventory.InventoryUpdated -= OnInventoryUpdate;

            StopRestore();

            base.OnStopServer();
        }

        public List<ResultContent> GetContent()
        {
            var output = new List<ResultContent>();
            
            const float begin = 0;

            List<Content> content = new();

            if (_content.ContentList != null)
            {
                content = new List<Content>(_content.ContentList);
            }

            float tempBegin = 0, tempEnd = 0;

            for (var i = 0; i < _content.ContentQuantity; i++)
            {
                var end = content.Sum(c => c.Probability);

                var random = Random.Range(begin, end);

                for (var b = 0; b < content.Count; b++)
                {
                    tempEnd += content[b].Probability;

                    if (random >= tempBegin && random <= tempEnd)
                    {
                        output.Add(new ResultContent(
                            content[b].Item,
                            Random.Range(content[b].MinQuantity, content[b].MaxQuantity + 1),
                            Random.Range(content[b].MinStockStrength, content[b].MaxStockStrength)));
                        
                        content.RemoveAt(b);
                        break;
                    }

                    tempBegin += content[b].Probability;
                }

                tempBegin = 0;
                tempEnd = 0;
            }

            return output;
        }
        
        [ServerCallback]
        protected virtual void OnFill()
        {
            Fill();
        }
        
        [ServerCallback]
        protected void Fill()
        {
            Inventory.Clear();

            var content = GetContent();

            foreach (var c in content)
            {
                InstantiateItem(c.Item, c.Quantity, c.StockStrength);
            }
        }

        [ServerCallback]
        private void InstantiateItem(
            BaseItem item,
            int quantity,
            float stockStrength)
        {
            var output = BaseItem.Instantiate(item);
            output.Quantity = quantity;
            output.StockStrength = stockStrength;
            
            Inventory.Add(output);
        }

        [ServerCallback]
        private void StartRestore()
        {
            _restore ??= StartCoroutine(Restore());
        }

        [ServerCallback]
        private void StopRestore()
        {
            if (_restore != null)
            {
                StopCoroutine(_restore);
                _restore = null;
            }
        }

        [ServerCallback]
        private IEnumerator Restore()
        {
            _isRestoring = true;
            _remainingTimeToRestore = _restoreTime;

            while (_remainingTimeToRestore > 0)
            {
                _remainingTimeToRestore--;

                yield return new WaitForSeconds(1);
            }
            
            _isRestoring = false;
            _remainingTimeToRestore = 0;
            
            OnFill();

            _restore = null;
        }
        
        #region Callbacks
        
        [ServerCallback]
        private void OnInventoryUpdate(SyncList<BaseItem>.Operation op, int index, BaseItem oldValue, BaseItem newValue)
        {
            StartRestore();
        }

        [ClientCallback]
        private void OnRestoringUpdated(bool _, bool newValue)
        {
            MiningDetectionHandler.enabled = !_isRestoring;

            if (_containerLidTransform != null)
            {
                _containerLidTransform.localPosition = newValue ?
                    _containerLidTransform.localPosition + _containerLidTransform.right * 0.02f : _containerLidStartPosition;
                
                _containerLidTransform.localEulerAngles = new Vector3(0, newValue ? -10 : 0, 0);
            }
        }
        
        [ClientCallback]
        protected virtual void OnRemainingTimeToRestoreUpdated(float oldValue, float newValue)
        {
            
        }
        
        #endregion
    }
}
