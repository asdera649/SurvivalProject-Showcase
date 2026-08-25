using FMODUnity;
using Mirror;
using UnityEngine;

namespace SP.Runtime.Core.Entities
{
    public class LootableEntityMediator : BuildingEntity
    {
        // Проблема: как я понял, когда в дженерик классе есть Command/ClientRPC (поправка: если при этом,
        // у класса который наследует этот дженерик класс тоже есть Command/ClientRPC), Mirror
        // не может сгенерировать свой код, жалуясь на то что якобы метод с таким именем уже есть в этом классе.
        // В консоле такая ошибка: ...have the same hash. Please rename one of them. To save bandwidth,
        // we only use 2 bytes for the hash, which has a small chance of collisions.
        // Решение: создать этот класс медиатор и поместить сюда необходимые Command/ClientRPC.
        // Неуверен на 100% насчет этого решения, может были варианты получше, но пока так.
        
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _openEventEmitter;
        [SerializeField] private StudioEventEmitter _closeEventEmitter;
        
        [Command(requiresAuthority = false)]
        protected void CmdInvokeOpenOrCloseImpacts(bool isOpen)
        {
            RpcInvokeOpenOrCloseImpacts(isOpen);
        }
        
        [ClientRpc]
        private void RpcInvokeOpenOrCloseImpacts(bool isOpen)
        {
            if (isOpen)
            {
                _openEventEmitter.Play();
            }
            else
            {
                _closeEventEmitter.Play();
            }
        }
    }
}