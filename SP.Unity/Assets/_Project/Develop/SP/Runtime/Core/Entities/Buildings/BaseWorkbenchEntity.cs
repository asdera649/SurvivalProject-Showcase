using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings
{
    public class BaseWorkbenchEntity : BuildingEntity
    {
        [Header("Settings")]
        [SerializeField] private int _workbenchLevel;
        public int WorkbenchLevel => _workbenchLevel;
    }
}