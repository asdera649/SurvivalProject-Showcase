using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Systems.LOD
{
    public enum LodDistanceMode
    {
        // Старое поведение: значения в _lodDistances - это screenRelativeTransitionHeight (0..1).
        // Реальная дистанция переключения зависит от размера объекта.
        RelativeScreenSize,

        // Новое поведение: значения в _lodDistances - это метры.
        // Дистанция переключения одинакова для всех объектов, независимо от их размера.
        FixedWorldDistance
    }

    [CreateAssetMenu(menuName = "LOD/LayerSettings")]
    public class LayerSettings : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private LodDistanceMode _mode = LodDistanceMode.RelativeScreenSize;

        [FormerlySerializedAs("_transition")]
        [SerializeField] private float[] _lodDistances;

        public LodDistanceMode Mode => _mode;
        public IReadOnlyList<float> LODDistances => _lodDistances;
    }
}