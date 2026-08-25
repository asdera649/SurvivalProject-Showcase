using SP.Runtime.Utilities;
using UnityEditor;
using UnityEngine;

namespace SP.Runtime.Core.Systems.GrassGeneration.Editor
{
    [CustomEditor(typeof(GrassData))]
    public class GrassDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var grassData = (GrassData)target;

            if (GUILayout.Button("Bake"))
            {
                grassData.Fill();
            }
            
            if (GUILayout.Button("Clear"))
            {
                grassData.Clear();
            }
            
            GUILayout.BeginVertical("box");
            GUILayout.Label(
                "Data size: " + SizeFormatter.Format(grassData.DataSize) + ", grasses count: " + grassData.GrassCount);
            GUILayout.EndVertical();
        }
    }
}
