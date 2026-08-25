using UnityEngine;

namespace SP.Runtime.Core.Systems.GrassGeneration
{
    [RequireComponent(typeof(LODGroup))]
    public class Grass : MonoBehaviour
    {
        [SerializeField] private GrassData.GrassType _grassType;
        public GrassData.GrassType GrassType => _grassType;
        
        private LODGroup _lodGroup;
        public LODGroup LODGroup
        {
            get
            {
                if (_lodGroup == null)
                {
                    _lodGroup = GetComponent<LODGroup>();
                }

                return _lodGroup;
            }
        }
    }
}