using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.UI.Drowning;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Water.Drowning
{
    [RequireComponent(typeof(Character))]
    public class DrowningHandler : NetworkBehaviour
    {
        [Header("Prefabs")] 
        [SerializeField] private DrowningTip _drowningTipPrefab;
        
        private float _distanceToHead;
        
        private DrowningTip _drowningTip;
        
        private Character _character;
        private Character Character
        {
            get
            {
                if (_character == null)
                {
                    _character = GetComponent<Character>();
                }

                return _character;
            }
        }

        private void Awake()
        {
            _distanceToHead = Vector3.Distance(transform.position, Character.HeadPosition);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            _drowningTip = Instantiate(_drowningTipPrefab, Loader.Instance.Canvas.transform);
        }

        public override void OnStopLocalPlayer()
        {
            if (_drowningTip != null)
            {
                Destroy(_drowningTip.gameObject);
            }
            
            base.OnStopLocalPlayer();
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                UpdateClientDrowning();
            }

            if (isServer)
            {
                UpdateServerDrowning();
            }
        }

        [ClientCallback]
        private void UpdateClientDrowning()
        {
            var drowningPercentage = GetDrowningPercentage();
            
            _drowningTip.gameObject.SetActive(drowningPercentage > 0);

            _drowningTip.DrowningPercentage = drowningPercentage;
        }
        
        [ServerCallback]
        private void UpdateServerDrowning()
        {
            if (GetDrowningPercentage() == 1)
            {
                Character.TakeDamage(
                    new BaseEntity.DamageSenderInfo(Character),
                    Character.MaxHealth,
                    DeathMethods.DeathByDrowning);
            }
        }
        
        private float GetDrowningPercentage()
        {
            var distance = 0f;
            
            if (Water.TryGetDistanceToWaterLevel(transform.position, out var output))
            {
                distance = output;
            }
            
            return Mathf.Clamp01(distance / _distanceToHead);
        }
    }
}