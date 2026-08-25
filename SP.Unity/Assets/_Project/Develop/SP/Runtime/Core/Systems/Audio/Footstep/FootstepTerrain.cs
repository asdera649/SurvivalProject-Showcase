using UnityEngine;

namespace SP.Runtime.Core.Systems.Audio.Footstep
{
    [RequireComponent(typeof(MeshRenderer))]
    public class FootstepTerrain : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Sprite _splatMap;
        [SerializeField] private Texture2D _redChanelTexture;
        [SerializeField] private Texture2D _greenChanelTexture;
        [SerializeField] private Texture2D _blueTexture;

        private float _terrainWidth;
        private float _terrainHeight;
        
        private int _splatWidth;
        private int _splatHeight;

        private MeshRenderer _meshRenderer;
        private MeshRenderer MeshRenderer
        {
            get
            {
                if (_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }

                return _meshRenderer;
            }
        }

        private void Awake()
        {
            _terrainWidth = MeshRenderer.bounds.size.x;
            _terrainHeight = MeshRenderer.bounds.size.z;
            
            _splatWidth = _splatMap.texture.width;
            _splatHeight = _splatMap.texture.height;
        }

        public Texture2D GetTerrainTexture(Vector3 position)
        {
            var output = _redChanelTexture;

            var splatMapCoordinate = ConvertToSplatMapCoordinate(position);
            
            var color = _splatMap.texture.GetPixel((int)splatMapCoordinate.x, (int)splatMapCoordinate.z);

            var currentChanel = 0f;

            if (color.r > currentChanel)
            {
                output = _redChanelTexture;
                currentChanel = color.r;
            }
            
            if (color.g > currentChanel)
            {
                output = _greenChanelTexture;
                currentChanel = color.g;
            }
            
            if (color.b > currentChanel)
            {
                output = _blueTexture;
            }
            
            return output;
        }

        private Vector3 ConvertToSplatMapCoordinate(Vector3 worldPosition)
        {
            return new Vector3(
                (worldPosition.x - transform.position.x) / _terrainWidth * _splatWidth,
                0,
                (worldPosition.z - transform.position.z) / _terrainHeight * _splatHeight);
        }
    }
}