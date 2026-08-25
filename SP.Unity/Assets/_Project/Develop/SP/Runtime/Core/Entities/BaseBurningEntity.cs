using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using UnityEngine;

namespace SP.Runtime.Core.Entities
{
    public class BaseBurningEntity : LootableEntity<BurningEntityInventoryBlock>
    {
        [Header("Settings")]
        [SerializeField] private float _tickTime = 10;
        [SerializeField] private BaseItem[] _fuelPool;

        [SyncVar(hook = nameof(OnEnableUpdate))] 
        private bool _isEnable;
        public bool IsEnable
        {
            get => _isEnable;
            private set
            {
                var temp = _isEnable;
                _isEnable = value;

                OnEnableUpdate(temp, _isEnable);
            }
        }
        
        [ServerCallback]
        private void UpdateTick()
        {
            if (ContainsFuel())
            {
                UpdateBurningTick();
                OnTickUpdate();
            }
            else
            {
                IsEnable = false;
            }

            if (!ContainsFuel())
            {
                IsEnable = false;
            }
        }

        [ServerCallback]
        private void UpdateBurningTick()
        {
            if (TryGetFuelItem(out var fuelItem))
            {
                fuelItem.Quantity--;
                
                var output = BaseItem.Instantiate(fuelItem.CombustionResidue);
                Inventory.Add(output);
            }
        }

        [ServerCallback]
        protected virtual void OnTickUpdate()
        { 
        
        }

        [ClientCallback]
        public void Enable()
        {
            CmdEnable();
        }

        [Command(requiresAuthority = false)]
        private void CmdEnable()
        {
            if (IsEnable)
            {
                IsEnable = false;
            }
            else
            {
                if (ContainsFuel())
                {
                    IsEnable = true;
                }
            }
        }

        #region Utilities

        private bool IsFuelItem(BaseItem item)
        {
            foreach (var f in _fuelPool)
            {
                if (f.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }
        
        private bool ContainsFuel()
        {
            foreach (var f in _fuelPool)
            {
                if (Inventory.Contains(f, 1))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetFuelItem(out BaseItem output)
        {
            output = null;
            
            foreach (var i in Inventory.GetInventory)
            {
                if (i != null && IsFuelItem(i))
                {
                    output = i;
                    break;
                }
            }

            return output != null;
        }

        #endregion

        #region Callbacks
        
        protected virtual void OnEnableUpdate(bool oldValue, bool newValue)
        {
            if (isServer)
            {
                if (newValue)
                {
                    InvokeRepeating(nameof(UpdateTick), _tickTime, _tickTime);
                }
                else
                {
                    CancelInvoke(nameof(UpdateTick));
                }
            }
        }
        
        #endregion
    }
}
