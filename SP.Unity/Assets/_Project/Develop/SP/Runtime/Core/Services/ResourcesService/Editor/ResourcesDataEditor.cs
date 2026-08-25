using UnityEditor;
using UnityEngine;

namespace SP.Runtime.Core.Services.ResourcesService.Editor
{
    [CustomEditor(typeof(ResourcesData))]
    public class ResourcesDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var resourcesData = (ResourcesData)target;

            if (GUILayout.Button("Fill"))
            {
                resourcesData.Fill();
            }
            
            if (GUILayout.Button("Clear"))
            {
                resourcesData.Clear();
            }
            
            if (GUILayout.Button("Instantiate"))
            {
                resourcesData.Instantiate();
            }
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("Total count: " + resourcesData.ResourceContainersCount);
            GUILayout.EndVertical();
        }
    }
}