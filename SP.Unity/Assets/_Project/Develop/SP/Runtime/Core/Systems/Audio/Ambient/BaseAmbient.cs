using Cinemachine;
using FMODUnity;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Audio.Ambient
{
    public class BaseAmbient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachinePathBase[] _cinemachinePaths;

        private float _position;
        private CinemachinePathBase _currentPath;
        private const CinemachinePathBase.PositionUnits _positionUnits = CinemachinePathBase.PositionUnits.PathUnits;

        protected static bool TryGetListener(out Transform output)
        {
            output = null;

            if (StudioListener.Listeners.Count == 0)
            {
                return false;
            }

            output = StudioListener.Listeners[0].AttenuationObject != null
                ? StudioListener.Listeners[0].AttenuationObject.transform
                : StudioListener.Listeners[0].transform;

            return output != null;
        }

        protected void SetCartPosition()
        {
            if (!TryGetListener(out var listener))
            {
                return;
            }

            if (_cinemachinePaths == null || _cinemachinePaths.Length == 0)
            {
                return;
            }

            FindClosestPath(listener.position, out _currentPath, out _position);

            transform.position = _currentPath.EvaluatePositionAtUnit(_position, _positionUnits);
            transform.rotation = _currentPath.EvaluateOrientationAtUnit(_position, _positionUnits);
        }

        private void FindClosestPath(
            Vector3 listenerPosition,
            out CinemachinePathBase closestPath,
            out float closestPosition)
        {
            closestPath = null;
            closestPosition = 0f;
            
            var closestDistance = float.MaxValue;

            foreach (var path in _cinemachinePaths)
            {
                if (path == null)
                {
                    continue;
                }

                var distanceAlongPath = path.FindClosestPoint(
                    listenerPosition,
                    0,
                    -1,
                    10);
                
                var standardized = path.StandardizeUnit(distanceAlongPath, _positionUnits);
                var pointOnPath = path.EvaluatePositionAtUnit(standardized, _positionUnits);
                var distance = Vector3.SqrMagnitude(listenerPosition - pointOnPath);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPath = path;
                    closestPosition = standardized;
                }
            }
        }
    }
}