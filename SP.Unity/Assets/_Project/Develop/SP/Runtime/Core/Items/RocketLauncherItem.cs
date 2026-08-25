using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Objects;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/RocketLauncherItem")]
    public class RocketLauncherItem : GunItem
    {
        [Header("Prefabs(RocketLauncherItem)")]
        [SerializeField] private Rocket _rocketPrefab;
        
        [Header("Settings(RocketLauncherItem)")]
        [SerializeField] private float _launchForce = 20;
    
        protected override void OnShot(Vector3 direction)
        {
            if (GunModel != null)
            {
                GunModel.Use();
            }
            
            if (!isServer)
            {
                return;
            }

            var rocket = Instantiate(_rocketPrefab, AimOriginPosition, Quaternion.LookRotation(direction));
            rocket.Initialize(new BaseEntity.DamageSenderInfo(Entity), DamageInfo);
            rocket.Rigidbody.AddForce(direction * _launchForce, ForceMode.Impulse);
            
            NetworkServer.Spawn(rocket.gameObject);
        }
    }
}
