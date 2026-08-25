using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/ShotgunItem")]
    public class ShotgunItem : GunItem
    {
        [Header("Settings(ShotgunItem)")]
        [SerializeField] private int _numberOfFractions;
        [SerializeField] private float _scatterOfFractions;

        private Vector3 _directionCache;

        protected override void OnShot(Vector3 direction)
        {
            _directionCache = direction;
            
            for (var i = 0; i < _numberOfFractions; i++)
            {
                direction = _directionCache;
                
                direction += new Vector3(
                    Random.Range(-_scatterOfFractions, _scatterOfFractions),
                    Random.Range(-_scatterOfFractions, _scatterOfFractions),
                    Random.Range(-_scatterOfFractions, _scatterOfFractions));
            
                Shot(direction);
            }
            
            if (GunModel != null)
            {
                GunModel.Use();
            }
        }
    }
}
