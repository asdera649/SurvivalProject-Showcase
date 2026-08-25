using System;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Items.Settings
{
    [CreateAssetMenu(menuName = "Items/BonesWeightSetting")]
    public class BonesWeightSettings : ScriptableObject
    {
        #region Structs
        
        [Serializable]
        public class Bone
        {
            [FormerlySerializedAs("BoneName")] 
            [SerializeField] private string _boneName;
            public string BoneName => _boneName;
            
            [FormerlySerializedAs("BoneWeight")] 
            [SerializeField] 
            [Range(0, 1)] private float _boneWeight;
            public float BoneWeight => _boneWeight;
        }
        
        #endregion

        [Header("Settings")]
        [SerializeField] private Bone[] _bones;

        public void SetupBones(IKSolverAim solver)
        {
            foreach (var b in solver.bones)
            {
                foreach (var bone in _bones)
                {
                    if (b.transform.name == bone.BoneName)
                    {
                        b.weight = bone.BoneWeight;
                        break;
                    }
                }
            }
        }
    }
}