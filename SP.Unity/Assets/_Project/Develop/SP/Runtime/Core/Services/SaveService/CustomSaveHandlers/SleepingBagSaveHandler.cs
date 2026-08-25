using SP.Runtime.Core.Entities.Buildings;
using UnityEngine;

namespace SP.Runtime.Core.Services.SaveService.CustomSaveHandlers
{
    [RequireComponent(typeof(SleepingBagEntity))]
    public class SleepingBagSaveHandler : MonoBehaviour, ISaveHandler
    {
        private SleepingBagEntity _sleepingBagEntity;
        private SleepingBagEntity SleepingBagEntity
        {
            get
            {
                if (_sleepingBagEntity == null)
                {
                    _sleepingBagEntity = GetComponent<SleepingBagEntity>();
                }

                return _sleepingBagEntity;
            }
        }
        
        public void OnSetSavedElements()
        {
            SleepingBagEntity.Initialize(SleepingBagEntity.OwnerUniqueId);
        }
    }
}