using SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock;
using TMPro.EditorUtilities;
using UnityEditor;

namespace SP.Runtime.Meta.UI.MainMenu.Editor
{
    [CustomEditor(typeof(SettingDropdown), true)]
    public class SettingDropdownEditor : DropdownEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SettingDropdown.OnDropdownOpened)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SettingDropdown.OnDropdownClosed)));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}