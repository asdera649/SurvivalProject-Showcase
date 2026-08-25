using EasyBuildSystem.Features.Scripts.Editor.Inspector.Builder;
using UnityEditor;

namespace Demo.Scripts.Systems.Building.Builder.Editor
{
    [CustomEditor(typeof(SP.Runtime.Core.Systems.Building.Builder.Builder), true)]
    public class BuilderEditor : BuilderBehaviourInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SP.Runtime.Core.Systems.Building.Builder.Builder.BuilderTipPrefab)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SP.Runtime.Core.Systems.Building.Builder.Builder.DelayForSpawnBuilderTip)));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}