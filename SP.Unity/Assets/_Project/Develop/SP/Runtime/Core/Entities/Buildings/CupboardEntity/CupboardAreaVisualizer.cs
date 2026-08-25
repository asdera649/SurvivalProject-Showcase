using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece.Enums;
using UnityEngine;
using UnityEngine.Rendering;

namespace SP.Runtime.Core.Entities.Buildings.CupboardEntity
{
    [RequireComponent(typeof(PieceBehaviour), typeof(CupboardEntity))]
    public class CupboardAreaVisualizer : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Material _visualizationMaterial;
        
        private PieceBehaviour _pieceBehaviour;
        private PieceBehaviour PieceBehaviour
        {
            get
            {
                if (_pieceBehaviour == null)
                {
                    _pieceBehaviour = GetComponent<PieceBehaviour>();
                }

                return _pieceBehaviour;
            }
        }
        
        private CupboardEntity _cupboardEntity;
        private CupboardEntity CupboardEntity
        {
            get
            {
                if (_cupboardEntity == null)
                {
                    _cupboardEntity = GetComponent<CupboardEntity>();
                }

                return _cupboardEntity;
            }
        }

        private void Start()
        {
            if (PieceBehaviour.CurrentState == StateType.Preview)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                sphere.name = nameof(CupboardAreaVisualizer);
                
                if (sphere.TryGetComponent(out SphereCollider c))
                {
                    Destroy(c);
                }
                
                if (sphere.TryGetComponent(out MeshRenderer r))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.material = _visualizationMaterial;
                }
                
                sphere.transform.parent = transform;
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localRotation = Quaternion.identity;
                sphere.transform.localScale = Vector3.one * CupboardEntity.CupboardArea.Radius * 2;
            }
        }
    }
}