using FMODUnity;
using UnityEngine;

namespace SP.Runtime.Core.Objects.Models
{
    public class BaseModel : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _pickEventEmitter;
        [SerializeField] private StudioEventEmitter _useEventEmitter;
        [SerializeField] private StudioEventEmitter _destructionEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private Vector3 _positionOffset;
        public Vector3 PositionOffset => _positionOffset;

        [SerializeField] private Vector3 _rotationOffset;
        public Vector3 RotationOffset => _rotationOffset;

        public void Pick()
        {
            _pickEventEmitter.Play();
        }
        
        public void Use()
        {
            _useEventEmitter.Play();
        }
        
        public void Destroy()
        {
            _destructionEventEmitter.Play();
        }
    }
}
