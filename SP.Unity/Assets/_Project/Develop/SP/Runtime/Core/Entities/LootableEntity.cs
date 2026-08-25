using System.Collections;
using Mirror;
using SP.Runtime.Core.Systems.Interaction;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using SP.Runtime.Core.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Entities
{
    #region Structs
    
    public class DistanceDestroyedObjectFactory
    {
        public DistanceDestroyedObjectFactory(float distance)
        {
            _distance = distance;
        }
        
        private readonly float _distance;
        
        public T Instantiate<T>(MonoBehaviour mono, Transform from, Transform to, T original) where T : Object
        {
            var output = Object.Instantiate(original);
            mono.StartCoroutine(CheckDistance(from, to, output));

            return output;
        }
            
        private IEnumerator CheckDistance(Transform from, Transform to, Object target)
        {
            while (true)
            {
                if (target == null)
                {
                    yield break;
                }
                    
                if (Vector3.Distance(from.position, to.position) > _distance)
                {
                    Object.Destroy(target);
                    yield break;
                }
                        
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
    
    #endregion
    
    [RequireComponent(typeof(Systems.Inventory.Inventory), typeof(InteractionHandler))]
    public class LootableEntity<T> : LootableEntityMediator where T: BaseAdditionalBlock
    {
        [Header("Prefabs")]
        [SerializeField] private T _additionalBlockPrefab;
        [SerializeField] private ResidualContainerEntity _residualContainerPrefab;
        
        [Header("Settings")] 
        [SerializeField] private string _additionalBlockNameEntry;
        [SerializeField] private float _lootableDistance = 3;
        
        [FormerlySerializedAs("_inventorySize")] 
        [SerializeField] protected int[] inventorySize = new int[] { 0 };
        
        private InteractionHandler _interactionHandler;
        protected InteractionHandler InteractionHandler
        {
            get
            {
                if (_interactionHandler == null)
                {
                    _interactionHandler = GetComponent<InteractionHandler>();
                }

                return _interactionHandler; 
            }
        }
        
        private Systems.Inventory.Inventory _inventory;
        protected Systems.Inventory.Inventory Inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = GetComponent<Systems.Inventory.Inventory>();
                }

                return _inventory;
            }
        }

        private T _additionalBlock;
        private DistanceDestroyedObjectFactory _distanceDestroyedObjectFactory;
        
        protected override void Awake()
        {
            base.Awake();
            
            _distanceDestroyedObjectFactory = new DistanceDestroyedObjectFactory(_lootableDistance);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            InitializeInventory();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            InteractionHandler.ExecutionAction += OnExecution;
        }

        public override void OnStopClient()
        {
            InteractionHandler.ExecutionAction -= OnExecution;
            
            DestroyAdditionalBlock();
            
            base.OnStopClient();
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            SpawnResidualContainer();
            
            base.OnDeath();
        }

        [ServerCallback]
        private void InitializeInventory()
        {
            var overallSize = 0;

            foreach (var s in inventorySize)
            {
                overallSize += s;
            }

            Inventory.Initialize(overallSize);
        }

        [ServerCallback]
        private void SpawnResidualContainer()
        {
            ResidualContainerEntity.SpawnResidualContainer(
                _residualContainerPrefab,
                ObjectUtils.CalculateCenter(transform),
                Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, 0)),
                Inventory);
        }

        protected virtual void OnExecution()
        {
            InstantiateAdditionalBlock();
        }

        private void InstantiateAdditionalBlock()
        {
            if (_additionalBlock != null)
            {
                return;
            }

            if (NetworkClient.localPlayer == null ||
                !NetworkClient.localPlayer.TryGetComponent(out Player.Player player))
            {
                return;
            }
            
            _additionalBlock = _distanceDestroyedObjectFactory.Instantiate(
                this,
                player.transform,
                transform,
                _additionalBlockPrefab.gameObject).GetComponent<T>();

            _additionalBlock.Destroyed += OnAdditionalBlockDestroyed;
            
            _additionalBlock.Initialize(this, _additionalBlockNameEntry, Inventory, inventorySize);
            
            player.SetInventoryMenuView(true, _additionalBlock);

            CmdInvokeOpenOrCloseImpacts(true);
        }

        private void DestroyAdditionalBlock()
        {
            if (_additionalBlock == null)
            {
                return;
            }
            
            Destroy(_additionalBlock.gameObject);
            _additionalBlock = null;
        }

        private void OnAdditionalBlockDestroyed(BaseAdditionalBlock additionalBlock)
        {
            additionalBlock.Destroyed -= OnAdditionalBlockDestroyed;

            if (!NetworkClient.active)
            {
                return;
            }
            
            CmdInvokeOpenOrCloseImpacts(false);
        }
    }
}
