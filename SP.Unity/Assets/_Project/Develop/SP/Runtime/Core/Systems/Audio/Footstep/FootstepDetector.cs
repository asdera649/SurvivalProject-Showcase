using System.Linq;
using FMODUnity;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Audio.Footstep
{
    [RequireComponent(typeof(Animator))]
    public class FootstepDetector : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _footstepsEventEmitter;
        [SerializeField] private StudioEventEmitter _landingEventEmitter;
        
        [Header("Settings")] 
        [SerializeField] private FootstepSurfaceSettings _footstepSurfaceSettings;

        [SerializeField] private float _heightToMarkOffGround = 0.17f;
        [SerializeField] private float _heightToMarkOnGround = 0.17f;
        
        [SerializeField] private float _raycastVerticalOffset = 1.3f;
        [SerializeField] private float _raycastCorrection = 0.2f;
        [SerializeField] private float _minTimeBetweenFootsteps = 0.22f;
        
        private readonly string _surfaceParameter = "Surface";

        private float _timeSinceLastFootstep;
        
        private float _originalLFHeight;
        private float _originalRFHeight;
        private Transform _LFTransform;
        private Transform _RFTransform;
        private float _previousLFHeight;
        private float _previousRFHeight;
        private bool _LFOffGround;
        private bool _RFOffGround;

        private Animator _animator;
        private Animator Animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponent<Animator>();
                }

                return _animator;
            }
        }

        private void Awake()
        {
            _originalLFHeight = Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y - transform.position.y;
            _originalRFHeight = Animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y - transform.position.y;

            _LFTransform = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _RFTransform = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        private void LateUpdate()
        {
            CheckLeftFoot();
            CheckRightFoot();
            
            _timeSinceLastFootstep += Time.deltaTime;
        }

        #region AnimationEvents
        
        public void LeftFootstep() 
        {
            //Footstep();
        }

        public void RightFootstep() 
        {
            //Footstep();
        }
        
        public void Landing()
        {
            if (TryGetUnderfootSurface(out var surface))
            {
                _landingEventEmitter.Play();
                _landingEventEmitter.SetParameter(_surfaceParameter, EnumUtils.GetValueIndex(surface));
            }
        }
        
        #endregion
        
        private void CheckLeftFoot() 
        {
            var currentLFHeight = _LFTransform.position.y - transform.position.y;
            var LFVelocity = currentLFHeight - _previousLFHeight;
            
            _previousLFHeight = currentLFHeight;

            if (!_LFOffGround && currentLFHeight > _originalLFHeight + _heightToMarkOffGround) 
            {
                _LFOffGround = true;
            }

            if (LFVelocity < 0 && 
                currentLFHeight <= _originalLFHeight + _heightToMarkOnGround &&
                _LFOffGround && 
                _timeSinceLastFootstep > _minTimeBetweenFootsteps) 
            {
                Footstep();
                
                _LFOffGround = false;
                _timeSinceLastFootstep = 0;
            }
        }
        
        private void CheckRightFoot() 
        {
            var currentRFHeight = _RFTransform.position.y - transform.position.y;
            var RFVelocity = currentRFHeight - _previousRFHeight;
            
            _previousRFHeight = currentRFHeight;

            if (!_RFOffGround && currentRFHeight > _originalRFHeight + _heightToMarkOffGround) 
            {
                _RFOffGround = true;
            }
            
            if (RFVelocity < 0 && 
                currentRFHeight <= _originalRFHeight + _heightToMarkOnGround &&
                _RFOffGround &&
                _timeSinceLastFootstep > _minTimeBetweenFootsteps) 
            {
                Footstep();
                
                _RFOffGround = false;
                _timeSinceLastFootstep = 0;
            }
        }
        
        private void Footstep()
        {
            if (TryGetUnderfootSurface(out var surface))
            {
                _footstepsEventEmitter.Play();
                _footstepsEventEmitter.SetParameter(_surfaceParameter, EnumUtils.GetValueIndex(surface), true);
            }
        }

        private bool TryGetUnderfootSurface(out FootstepSurfaceSettings.Surface output)
        {
            output = FootstepSurfaceSettings.Surface.Grass;
            
            var hits = Physics.RaycastAll(
                transform.position + new Vector3(0, _raycastVerticalOffset, 0),
                Vector3.down,
                _raycastVerticalOffset + _raycastCorrection,
                -1,
                QueryTriggerInteraction.Collide).OrderBy(h => h.distance);

            foreach (var h in hits)
            {
                if (h.collider.isTrigger &&
                    h.transform.gameObject.layer != LayerUtils.GetWaterLayer())
                {
                    continue;
                }

                if (h.transform.TryGetComponent<FootstepTerrain>(out var footstepTerrain))
                {
                    output = _footstepSurfaceSettings.GetSurfaceByTexture(footstepTerrain.GetTerrainTexture(h.point));
                    
                    return true;
                }

                var meshRenderer = h.transform.GetComponentInChildren<MeshRenderer>();
                
                if (meshRenderer != null &&
                    meshRenderer.sharedMaterial != null)
                {
                    output = _footstepSurfaceSettings.GetSurfaceByMaterial(meshRenderer.sharedMaterial);

                    return true;
                }
            }

            return false;
        }
    }
}