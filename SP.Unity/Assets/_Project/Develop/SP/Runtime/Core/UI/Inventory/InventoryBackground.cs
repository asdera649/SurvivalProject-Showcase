using UnityEngine;
using UnityEngine.EventSystems;

namespace SP.Runtime.Core.UI.Inventory
{
    public class InventoryBackground : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private static bool _isPointerDown;
        public static bool IsPointerDown => _isPointerDown;

        private static bool _isDoublePointerDown;
        public static bool IsDoublePointerDown => _isDoublePointerDown;

        public void OnDisable()
        {
            _isPointerDown = false;
            _isDoublePointerDown = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isPointerDown)
                _isDoublePointerDown = true;
            else
                _isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDoublePointerDown)
                _isDoublePointerDown = false;
            else
                _isPointerDown = false;
        }
    }
}
