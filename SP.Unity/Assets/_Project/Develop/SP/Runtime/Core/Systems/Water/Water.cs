using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Water
{
    [RequireComponent(typeof(BoxCollider))]
    public class Water : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _waterDepth = 100;

        // Пока сделал такую простую систему воды, в которой возможно иметь
        // только один экземпляр воды на сцене, в будущем возможно придется дорабатывать.
        private static float _waterLevel;
        private static bool _isWaterLevelInitialized;
        
        private BoxCollider _boxCollider;
        private BoxCollider BoxCollider
        {
            get
            {
                if (_boxCollider == null)
                {
                    _boxCollider = GetComponent<BoxCollider>();
                }
                
                return _boxCollider;
            }
        }

        private void Awake()
        {
            InitializeWaterLevel();
        }

        private void InitializeWaterLevel()
        {
            if (_isWaterLevelInitialized)
            {
                Debug.LogWarning("Re-attempt to initialize the height of the water!");
                return;
            }

            _waterLevel = transform.position.y;
            _isWaterLevelInitialized = true;
        }

        private void OnDestroy()
        {
            _waterLevel = 0;
            _isWaterLevelInitialized = false;
        }

        public void OnValidate()
        {
            var waterLayer = LayerUtils.GetWaterLayer();

            if (BoxCollider.gameObject.layer != waterLayer)
            {
                BoxCollider.gameObject.layer = waterLayer;
            }
            
            BoxCollider.isTrigger = true;
            BoxCollider.center = new Vector3(0, _waterDepth / -2, 0);
            BoxCollider.size = new Vector3(BoxCollider.size.x, _waterDepth, BoxCollider.size.z);
        }

        public static bool TryGetDistanceToWaterLevel(Vector3 position, out float output)
        {
            output = 0;

            var value = position.y < _waterLevel;
            
            if (value)
            {
                output = _waterLevel - position.y;
            }

            return _isWaterLevelInitialized && value;
        }
    }
}