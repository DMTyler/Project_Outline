using System.Collections.Generic;
using DGraphics.Inspector;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Outline.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(FixedListAttribute))]
    public class FixedListDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!IsArrayOrList(property))
            {
                EditorGUI.LabelField(position, label.text, "FixedList only works with arrays or List<> fields.");
                return;
            }
            var reorderableList = GetReorderableList(property, label);
            
            property.serializedObject.Update();
            reorderableList.DoList(position);
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsArrayOrList(property))
            {
                // 如果不是数组或 List，就只占一行高度
                return EditorGUIUtility.singleLineHeight;
            }

            var reorderableList = GetReorderableList(property, null);
            return reorderableList.GetHeight();
        }
        
        private bool IsArrayOrList(SerializedProperty property)
        {
            return property.isArray && property.propertyType == SerializedPropertyType.Generic;
        }
        
        private ReorderableList GetReorderableList(SerializedProperty property, GUIContent label)
        {
            if (!_reorderableLists.TryGetValue(property.propertyPath, out var list))
            {
                list = new ReorderableList(
                    property.serializedObject,
                    property,
                    draggable: true,
                    displayHeader: true,
                    displayAddButton: false,
                    displayRemoveButton: false
                );
                
                list.drawHeaderCallback = (Rect rect) =>
                {
                    var header = label != null ? label.text : property.displayName;
                    EditorGUI.LabelField(rect, header);
                };
                
                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element,
                        GUIContent.none
                    );
                };
                _reorderableLists[property.propertyPath] = list;
            }

            return list;
        }
    }
}