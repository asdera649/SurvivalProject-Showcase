using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Systems.LOD
{
    public class LodLayer : MonoBehaviour
    {
        [Header("References")]
        [FormerlySerializedAs("_renderers")]
        [SerializeField] private GameObject[] _objects;

        [Header("Settings")]
        [FormerlySerializedAs("_layerSetting")]
        [SerializeField] private LayerSettings _layerSettings;

        private LODGroup _lodGroup;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lodGroup = gameObject.AddComponent<LODGroup>();

            var renderersPerLod = new Renderer[_objects.Length][];
            
            for (var i = 0; i < _objects.Length; i++)
            {
                renderersPerLod[i] = _objects[i].GetComponentsInChildren<Renderer>();
            }

            if (_layerSettings.Mode == LodDistanceMode.RelativeScreenSize)
            {
                // Старое поведение: значения из настроек - это уже готовые screenRelativeTransitionHeight.
                var lods = BuildLods(renderersPerLod, index => index < _layerSettings.LODDistances.Count
                    ? _layerSettings.LODDistances[index]
                    : 0f);

                _lodGroup.SetLODs(lods);
                return;
            }

            // Новое поведение (FixedWorldDistance): значения из настроек - это метры.
            // Чтобы перевести метры в screenRelativeTransitionHeight,
            // нужен world-space размер объекта (LODGroup.size), а его Unity считает
            // автоматически только после SetLODs(). Поэтому делаем два прохода:
            //
            // 1) SetLODs с любыми валидными - чтобы Unity
            //    посчитала реальные size/localReferencePoint.
            // 2) Читаем посчитанный size, конвертируем метры в screenRelativeTransitionHeight
            //    и вызываем SetLODs повторно уже с правильными порогами.
            var placeholderLods = BuildLods(renderersPerLod, index => Mathf.Max(0.01f, 1f - index * 0.1f));
            _lodGroup.SetLODs(placeholderLods);
            
            _lodGroup.RecalculateBounds();

            var worldSize = _lodGroup.size * GetMaxScaleComponent(transform.lossyScale);

            var finalLods = BuildLods(renderersPerLod, index => index < _layerSettings.LODDistances.Count
                ? GetScreenRelativeHeight(_layerSettings.LODDistances[index], worldSize)
                : 0f);

            _lodGroup.SetLODs(finalLods);
        }

        private static UnityEngine.LOD[] BuildLods(Renderer[][] renderersPerLod, System.Func<int, float> heightForIndex)
        {
            var lods = new UnityEngine.LOD[renderersPerLod.Length];
            
            for (var i = 0; i < renderersPerLod.Length; i++)
            {
                lods[i] = new UnityEngine.LOD(heightForIndex(i), renderersPerLod[i]);
            }

            return lods;
        }

        private static float GetMaxScaleComponent(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private float GetScreenRelativeHeight(float distanceMeters, float worldSize)
        {
            var cam = UnityEngine.Camera.main;
            
            if (cam == null)
            {
                Debug.LogWarning($"[{nameof(LodLayer)}] Camera.main not found, " +
                                 $"FixedWorldDistance mode cannot calculate LOD for {name}.");
                return distanceMeters;
            }

            if (worldSize <= 0f)
            {
                return distanceMeters;
            }

            var distance = Mathf.Max(distanceMeters, 0.0001f);
            var fovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            
            var screenRelativeHeight = worldSize / (2f * distance * Mathf.Tan(fovRad));

            return Mathf.Clamp01(screenRelativeHeight);
        }
    }
}