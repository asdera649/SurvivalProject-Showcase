using FMODUnity;
using Mirror;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings.FurnaceEntity
{
    public class FurnaceEntity : BaseBurningEntity
    {
        [Header("References")] 
        [SerializeField] private BurnLightHandler _burnLightHandler;
        [SerializeField] private ParticleSystem _fire;
        [SerializeField] private ParticleSystem _smoke;
        [SerializeField] private StudioEventEmitter _burningEventEmitter;

        [ServerCallback]
        protected override void OnTickUpdate()
        {
            UpdateRemelting();
        }

        [ServerCallback]
        private void UpdateRemelting()
        {
            foreach (var i in Inventory.GetInventory) 
            {
                if (i != null && i.RemeltedItem != null)
                {
                    i.Quantity--;
                    
                    InstantiateItem(i.RemeltedItem);
                }
            }
        }

        [ServerCallback]
        private void InstantiateItem(BaseItem item)
        {
            var outputItem = BaseItem.Instantiate(item);
            
            Inventory.Add(outputItem);
        }

        #region Callbacks
        
        protected override void OnEnableUpdate(bool oldValue, bool newValue)
        {
            base.OnEnableUpdate(oldValue, newValue);

            if (!isClient)
            {
                return;
            }
            
            _burnLightHandler.SetActive(newValue);
            
            if (newValue)
            {
                _fire.Play();
                _smoke.Play();
                
                if (!_burningEventEmitter.IsPlaying())
                {
                    _burningEventEmitter.Play();
                }
            }
            else
            {
                _fire.Stop();
                _smoke.Stop();
                
                if (_burningEventEmitter.IsPlaying())
                {
                    _burningEventEmitter.Stop();
                }
            }
        }
        
        #endregion
    }
}
