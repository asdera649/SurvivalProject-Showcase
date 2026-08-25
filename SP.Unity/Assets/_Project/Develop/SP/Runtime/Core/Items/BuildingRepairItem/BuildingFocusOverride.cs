using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Core.Items.BuildingRepairItem
{
    public class BuildingFocusOverride : MonoBehaviour
    {
        [SerializeField] private List<Transform> _anchors = new();
        public IReadOnlyList<Transform> Anchors => _anchors;
    }
}