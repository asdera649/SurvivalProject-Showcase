using UnityEngine;

namespace SP.Runtime.Core.UI.MiniMap
{
    public class PlacemarkObject : MonoBehaviour
    {
        [field: Header("Settings")]
        [field: SerializeField] public Sprite PlacemarkIcon { get; private set; }
        [field: SerializeField] public string PlacemarkName { get; private set; }
        [field: SerializeField] public string PlacemarkDescription { get; private set; }

        private void Awake()
        {
            MiniMap.AddStartPlacemark(this);
        }

        private void OnDestroy()
        {
            MiniMap.RemoveStartPlacemark(this);
        }
    }
}
