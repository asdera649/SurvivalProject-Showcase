using UnityEditor;
using UnityEngine;

namespace SP.Runtime.Core.Entities.LootContainers.Editor
{
    [CustomEditor(typeof(ContainerEntity), true)]
    public class ContainerEntityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var container = (ContainerEntity)target;

            if (GUILayout.Button("Test-Fill"))
            {
                var content = container.GetContent();

                Debug.Log("Кол-во контента: " + content.Count);
                
                foreach (var c in content)
                {
                    Debug.Log(
                        "Предмет: " + c.Item.Name + ", кол-во: " + c.Quantity + ", прочность: " + c.StockStrength);
                }
            }
        }
    }
}