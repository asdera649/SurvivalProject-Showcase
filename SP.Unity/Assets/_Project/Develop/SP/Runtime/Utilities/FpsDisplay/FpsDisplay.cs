using UnityEngine;

namespace SP.Runtime.Utilities.FpsDisplay
{
    public class FpsDisplay : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private FpsDisplaySettings _fpsDisplaySettings;
        
        [Space(10)]
        
        [SerializeField] private Vector2 _position;
        [SerializeField] private int _fontSize = 24;
        [SerializeField] private Color _textColor = new (255, 255, 255, 100);

        private float _deltaTime;

        private GUIStyle _style;

        private void Awake()
        {
            _style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = _fontSize,
                normal =
                {
                    textColor = _textColor
                }
            };
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (!_fpsDisplaySettings.ShowFps)
            {
                return;
            }
            
            var position = new Rect(_position.x, Screen.height - _position.y, Screen.width - 200, (float)Screen.height * 2 / 100);
            
            var text = $"{_deltaTime * 1000:0.0} ms ({1 / _deltaTime:0.} fps)";

            GUI.Label(position, text, _style);
        }
    }
}

