using UnityEditor;
using UnityEngine;

namespace SP.Runtime.Core.Systems.GroundColorMapRendering.Editor
{
    [CustomEditor(typeof(GroundColorMapRenderer))]
    public class GroundColorMapRendererEditor : UnityEditor.Editor
    {
        private GroundColorMapRenderer _script;
    
        private static string _iconPrefix => EditorGUIUtility.isProSkin ? "d_" : "";
        private static GUIContent _renderButtonContent;

        private void OnEnable()
        {
            _script = (GroundColorMapRenderer)target;
        
            _renderButtonContent  = new GUIContent(
                "  Render",
                EditorGUIUtility.IconContent(_iconPrefix + "Animation.Record").image);
        }
    
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            if (_script.GroundObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("\n" +
                                        "Assign the target ground objects to the list above to render." +
                                        "\n\nThese can be Unity terrain objects, or regular Mesh Renderers." +
                                        "\n", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Calculate bounds from ground(s)", GUILayout.Height(30f)))
                {
                    _script.RecalculateBounds();
                }
            
                if (GUILayout.Button(_renderButtonContent, GUILayout.Height(30f)))
                {
                    _script.Render();
                }
            }
        }
    }
}
