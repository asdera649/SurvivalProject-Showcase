using SP.Runtime.Core.Systems.Destruction;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Mined.Tree
{
    [RequireComponent(typeof(MeshRenderer))]
    public class TreeDebrisPiece : DebrisPiece
    {
        [Header("Settings")]
        [SerializeField] private uint _branchMaterialIndex;
        
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

        public void SetMaterial(Material branchMaterial)
        {
            if (MeshRenderer.sharedMaterials.Length > _branchMaterialIndex)
            {
                var mat = MeshRenderer.sharedMaterials;

                mat[_branchMaterialIndex] = branchMaterial;

                MeshRenderer.sharedMaterials = mat;
            }
        }
    }
}