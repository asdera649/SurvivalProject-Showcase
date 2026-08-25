using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Hiding
{
    public class HidingHandlerGroup : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private bool _useCustomHiderBoxes;
        public bool UseCustomHiderBoxes => _useCustomHiderBoxes;
        
        [SerializeField] private Transform[] _customHiderBoxes;
        public IReadOnlyList<Transform> CustomHiderBoxes => _customHiderBoxes;
    }
}