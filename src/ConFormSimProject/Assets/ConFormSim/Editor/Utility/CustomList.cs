using UnityEditor;
using UnityEngine;

namespace ConFormSim.Editor
{
    public static class CustomList
    {
        public static float GetListHeight(SerializedProperty list, bool showListSize=true, bool alwaysExtended=false)
        {
            float newLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float height = EditorGUI.GetPropertyHeight(list, false);
            if(alwaysExtended)
            {
                list.isExpanded = true;
            }
            if (list.isExpanded)
            {
                if (showListSize)
                {
                    height += newLineSpace;
                }
                for(int i = 0; i < list.arraySize; i++)
                {
                    height += newLineSpace;
                }
            }
            return height;
        }
        
        public static void Show(Rect rect, SerializedProperty list, bool showListSize=true, bool alwaysExtended=false)
        {
            float newLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            rect.height = EditorGUI.GetPropertyHeight(list, false);
            EditorGUI.PropertyField(rect, list, false);
            if(alwaysExtended)
            {
                list.isExpanded = true;
            }
            EditorGUI.indentLevel++;
            
            if (list.isExpanded)
            {
                if (showListSize)
                {
                    rect.y += newLineSpace;
                    EditorGUI.PropertyField(rect, list.FindPropertyRelative("Array.size"));
                }
                for(int i = 0; i < list.arraySize; i++)
                {
                    rect.y += newLineSpace;
                    EditorGUI.PropertyField(rect, list.GetArrayElementAtIndex(i));
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}