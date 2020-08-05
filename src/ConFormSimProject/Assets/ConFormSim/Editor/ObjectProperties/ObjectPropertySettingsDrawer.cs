// Based on code
// Developed by Tom Kail at Inkle
// Released under the MIT Licence as held at https://opensource.org/licenses/MIT
// https://gist.github.com/tomkail/ba4136e6aa990f4dc94e0d39ec6a058c

using UnityEditor;
using UnityEngine;
using ConFormSim.ObjectProperties;

namespace ConFormSim.Editor
{
    [CustomPropertyDrawer(typeof(ObjectPropertySettings))]
    public class ObjectPropertySettingsDrawer : PropertyDrawer {
        private SerializedProperty typeProp;
        private SerializedProperty arrayLengthProp;
        private SerializedProperty defaultValueProp;
        private bool isArrayType = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {        
            FieldProperties(property);
            float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (isArrayType)
            {
                totalHeight += EditorGUI.GetPropertyHeight(arrayLengthProp, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            totalHeight += EditorGUI.GetPropertyHeight(defaultValueProp);
            // if (defaultValueProp.isExpanded)
            // {
            //     var data = defaultValueProp.objectReferenceValue as ScriptableObject;
            //     if( data == null ) return EditorGUIUtility.singleLineHeight;
            //     SerializedObject serializedObject = new SerializedObject(data);
            //     SerializedProperty prop = serializedObject.GetIterator();
            //     if (prop.NextVisible(true)) {
            //         do {
            //             if(prop.name == "m_Script") continue;
            //             var subProp = serializedObject.FindProperty(prop.name);
            //             float height;
            //             if (prop.name == "values")
            //             {
            //                 height = CustomList.GetListHeight(prop, false, false);
            //             }
            //             else
            //             {
            //                 height = EditorGUI.GetPropertyHeight(subProp, null, true) + EditorGUIUtility.standardVerticalSpacing;
            //             }
            //             totalHeight += height;
            //         }
            //         while (prop.NextVisible(false));
            //     }
            // }

            //totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FieldProperties(property);
            label = EditorGUI.BeginProperty(position, new GUIContent("Type"), property);
            Rect typePosition = EditorGUI.PrefixLabel(position, label);
            Rect aslistRect = new Rect(typePosition); 
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();
            // type property
            typePosition.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.ObjectField(typePosition, typeProp, typeof(ObjectProperty), GUIContent.none);
    
            if (isArrayType)
            {
                // got to a new line and create the value
                position.y += EditorGUI.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing; 
                EditorGUI.PropertyField(position, arrayLengthProp);
            }
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSettings(property);
            }

            EditorGUI.PropertyField(position, defaultValueProp);
            EditorGUI.EndProperty();
        }

        public void FieldProperties(SerializedProperty property)
        {
            typeProp = property.FindPropertyRelative("type");
            arrayLengthProp = property.FindPropertyRelative("arrayLength");
            defaultValueProp = property.FindPropertyRelative("defaultValue");
            ObjectPropertySettings settingsObj = 
                PropertyDrawerUtility.GetActualObjectForSerializedProperty<ObjectPropertySettings>(fieldInfo, property, new string[]{"_"});
            if (settingsObj.type != null)
            {
                isArrayType = settingsObj.type.isArrayType;
            }
            else
            {
                isArrayType = false;
            }
        }

        void UpdateSettings(SerializedProperty property)
        {
            property.serializedObject.ApplyModifiedProperties();
            ObjectPropertySettings settingsObj = 
                PropertyDrawerUtility.GetActualObjectForSerializedProperty<ObjectPropertySettings>(fieldInfo, property, new string[]{"_"});
            settingsObj.UpdateSettings();
        }
    }
}