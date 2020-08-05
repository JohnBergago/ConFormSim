using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using ConFormSim.ObjectProperties;

namespace ConFormSim.Editor
{
    [CustomEditor(typeof(ObjectPropertyProvider))]
    [CanEditMultipleObjects]
    public class ObjectPropertySOProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PropertyListData;
        private ReorderableList m_ReorderablePropList;

        private struct PropertyCreationParams {
            public string name;
            public ObjectPropertySettings settings;
        }

        void OnEnable()
        {
            // Find the list that will be reorderable
            m_PropertyListData = serializedObject.FindProperty("propertyList");

            // Create instance of reorderable list
            m_ReorderablePropList = new ReorderableList(serializedObject, m_PropertyListData, 
                draggable: true, displayHeader: true, displayAddButton: true, 
                displayRemoveButton:true);
            
            // Set up method callback to draw list header
            m_ReorderablePropList.drawHeaderCallback = DrawPropListHeaderCallback;

            // callback to draw each element of the list
            m_ReorderablePropList.drawElementCallback = DrawPropListElementCallback;

            // set height of each element
            m_ReorderablePropList.elementHeightCallback = DrawPropListHeightCallback;
            m_ReorderablePropList.onRemoveCallback = OnPropListRemoveCallback;
            m_ReorderablePropList.onAddDropdownCallback = OnPropListAddDropdownCallback;
            
            UpdateProperties();
        }

        private void DrawPropListHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, m_PropertyListData.displayName);
        }

        private void DrawPropListElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            // Get the element we want from the list
            SerializedProperty element = m_ReorderablePropList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            // get the name property so we can display this on the list
            SerializedProperty elementName = element.FindPropertyRelative("name");
            string elementTitle = string.IsNullOrEmpty(elementName.stringValue) 
                ? "Unnamed Property" 
                : elementName.stringValue;

            EditorGUI.PropertyField(
                new Rect(rect.x + 10, rect.y, Screen.width * 0.8f, EditorGUIUtility.singleLineHeight),
                property:element, new GUIContent(elementTitle), includeChildren: true);
        }

        private float DrawPropListHeightCallback(int index)
        {
            //Gets the height of the element. This also accounts for properties that can be expanded, like structs.
            float propertyHeight =
                EditorGUI.GetPropertyHeight(m_ReorderablePropList.serializedProperty.GetArrayElementAtIndex(index), true);

            float spacing = EditorGUIUtility.singleLineHeight / 2;

            return propertyHeight + spacing;
        }

        private void OnPropListRemoveCallback(ReorderableList list)
        {
            SerializedProperty element = m_ReorderablePropList.serializedProperty.GetArrayElementAtIndex(m_ReorderablePropList.index);
            string elementName = element.FindPropertyRelative("name").stringValue;
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            NamedObjectProperty elementProp;
            if (propertyProvider.PropertyDictionary.TryGetValue(elementName, out elementProp))
            {
                if (elementProp.property != null)
                {
                    DestroyImmediate(elementProp.property, true);
                }
            }
            propertyProvider.PropertyDictionary.Remove(elementName);

            if (m_ReorderablePropList.index <= m_ReorderablePropList.serializedProperty.arraySize - 1)
            {
                m_ReorderablePropList.index = m_ReorderablePropList.index;
            }
            UpdateProperties();
        }

        private void OnPropListAddDropdownCallback(Rect rect, ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();

            SerializedProperty availProps = serializedObject.FindProperty("availableProperties");
            FeatureVectorDefinition fvd = availProps.objectReferenceValue as FeatureVectorDefinition;
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            
            List<string> missingKeys = fvd.Properties.Keys.Except(propertyProvider.PropertyDictionary.Keys).ToList();
            menu.AddItem(new GUIContent("All"), false, OnAddAllMissingPropertiesToList, missingKeys);
            menu.AddSeparator("");
            foreach(string item in missingKeys)
            {
                ObjectPropertySettings value;
                fvd.Properties.TryGetValue(item, out value);

                menu.AddItem(new GUIContent(item), false, OnAddPropertyToList, new PropertyCreationParams(){name=item, settings=value});
            }
            menu.ShowAsContext();

        }

        void OnAddPropertyToList(object propertyParams)
        {
            PropertyCreationParams propParams = (PropertyCreationParams) propertyParams;
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            
            // create new object from settings
            ObjectProperty newProp=null;
            if (propParams.settings.type != null)
            {
                newProp = (ObjectProperty) Instantiate(propParams.settings.type);
                newProp.ApplySettings(propParams.settings);
            }
            NamedObjectProperty newNamedProp = new NamedObjectProperty(propParams.name, newProp);
            propertyProvider.PropertyDictionary.Add(propParams.name, newNamedProp);
            UpdateProperties();
        }

        void OnAddAllMissingPropertiesToList(object missingKeys)
        {
            List<string> items2Add = (List<string>) missingKeys;
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            
            foreach(string name in items2Add)
            {
                ObjectPropertySettings settings;
                propertyProvider.AvailableProperties.Properties.TryGetValue(name, out settings);
                // create new object from settings
                ObjectProperty newProp=null;
                if (settings.type != null)
                {
                    newProp = (ObjectProperty) Instantiate(settings.type);
                    newProp.ApplySettings(settings);
                }
                NamedObjectProperty newNamedProp = new NamedObjectProperty(name, newProp);
                propertyProvider.PropertyDictionary.Add(name, newNamedProp);
            }
            UpdateProperties();
        }

        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty("availableProperties"));
            
            so.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                ResetProperties();
            }
            so.Update();
            m_ReorderablePropList.DoLayoutList();
            so.ApplyModifiedProperties();

        }

        void UpdateProperties()
        {
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            propertyProvider?.UpdateProperties();
        }
        void ResetProperties()
        {
            var propertyProvider = serializedObject.targetObject as ObjectPropertyProvider;
            propertyProvider?.Reset();
        }
    }
}