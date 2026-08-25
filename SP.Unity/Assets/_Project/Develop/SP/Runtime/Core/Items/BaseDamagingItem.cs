using System;
using SP.Runtime.Core.Entities;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    #region Structs
    
    public enum DeathMethods
    {
        Null,
        DeathByFalling,
        DeathByAxe,
        DeathByPickaxe,
        DeathBySemiAutomaticPistol,
        DeathByLongSword,
        DeathByWoodenClub,
        DeathByDoubleBarrelShotgun,
        DeathByHandmadePistol,
        DeathByAssaultRifle,
        DeathByMachete,
        DeathByHandmadeExplosive,
        DeathByExplosiveBundle,
        DeathByTimedExplosive,
        DeathByRocketLauncher,
        DeathByStoneAxe,
        DeathByStonePickaxe,
        DeathByNailgun,
        DeathByHandmadeSubmachineGun,
        DeathBySubmachineGun,
        DeathByDrowning,
        DeathByStoneChunk
    }

    [Serializable]
    public class DamageToIndividualEntity
    {
        public BaseEntity Entity;
        public bool FindInInherited;
        public int Damage;
    }

    [Serializable]
    public class DamageInfo
    {
        public BaseEntity[] IgnoredEntities;
        public DeathMethods DeathMethod;
        public int Damage;
        public DamageToIndividualEntity[] DamageToIndividualEntities;
    }
    
    #endregion

    public class BaseDamagingItem : BaseItem
    {
        [Header("Settings(BaseDamagingItem)")]
        [SerializeField] private DamageInfo _damageInfo;
        protected DamageInfo DamageInfo => _damageInfo;
    
        protected BaseEntity Entity { private set; get; }

        protected override void OnInitialize(Systems.Inventory.Inventory owner)
        {
            base.OnInitialize(owner);
        
            Entity = owner.GetComponent<BaseEntity>();
        }

        protected override void OnDeinitialize()
        {
            Entity = null;

            base.OnDeinitialize();
        }

        protected bool SendDamage(BaseEntity targetEntity, float coefficient = 1)
        {
            if (!isServer)
            {
                return false;
            }

            if (Entity != null)
            {
                return SendDamage(targetEntity, _damageInfo, new BaseEntity.DamageSenderInfo(Entity), coefficient);
            }
            else
            {
                Debug.LogWarning("Entity is null!");
                return false;
            }
        }

        public static bool SendDamage(BaseEntity targetEntity, DamageInfo damageInfo, BaseEntity.DamageSenderInfo sender, float coefficient = 1)
        {
            var currentDamage = damageInfo.Damage;

            foreach (var e in damageInfo.IgnoredEntities)
            {
                if (targetEntity.GetType() == e.GetType())
                {
                    return false;
                }
            }

            foreach (var individualEntity in damageInfo.DamageToIndividualEntities)
            {
                if (!individualEntity.FindInInherited)
                {
                    if (targetEntity.GetType() == individualEntity.Entity.GetType())
                    {
                        currentDamage = individualEntity.Damage;
                        break;
                    }
                }
                else
                {
                    if (targetEntity.GetType().IsSubclassOf(individualEntity.Entity.GetType()))
                    {
                        currentDamage = individualEntity.Damage;
                        break;
                    }
                }
            }

            currentDamage = (int)Mathf.Lerp(0, currentDamage, coefficient);
            targetEntity.TakeDamage(sender, currentDamage, damageInfo.DeathMethod);
            return true;
        }
    }
}