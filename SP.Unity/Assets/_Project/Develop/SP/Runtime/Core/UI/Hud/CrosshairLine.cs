using UnityEngine;
using UnityEngine.UI.Extensions;

namespace SP.Runtime.Core.UI.Hud
{
    [RequireComponent(typeof(UILineRenderer))]
    public class CrosshairLine : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color _crosshairLineRedColor = Color.red;
        
        private Color _crosshairColorBackup;
        
        private UILineRenderer _lineRenderer;
        public UILineRenderer LineRenderer
        {
            get
            {
                if (_lineRenderer == null)
                {
                    _lineRenderer = GetComponent<UILineRenderer>();
                }

                return _lineRenderer;
            }
        }
        
        private void Awake()
        {
            _crosshairColorBackup = LineRenderer.color;
        }
        
        public void SetActiveRedCrosshair(bool value)
        {
            if (value)
            {
                LineRenderer.color = new Color(
                    _crosshairLineRedColor.r,
                    _crosshairLineRedColor.g,
                    _crosshairLineRedColor.b,
                    LineRenderer.color.a);
            }
            else
            {
                LineRenderer.color = _crosshairColorBackup;
            }
        }
    }
}