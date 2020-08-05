using UnityEngine;
using UnityEditor;
using System.Linq;
using ConFormSim.ObjectProperties;
using RotaryHeart.Lib.SerializableDictionary;
using System;

namespace ConFormSim.Editor
{
    [CustomEditor(typeof(FeatureVectorDefinition))]
    public class FeatureVectorDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PropertiesDict;
        private FeatureVectorDefinition fvdObject;
        private bool listDragged = false;
        private bool subscribedToEvent = false;

        void OnEnable()
        {
            m_PropertiesDict = serializedObject.FindProperty("properties");
            fvdObject = serializedObject.targetObject as FeatureVectorDefinition;
            subscribedToEvent=false;
        }

        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();
            // subscribe to the onAddCallback event of the reorderable list, as
            // sonn as the list was initialized
            if (! subscribedToEvent && fvdObject.Properties.reorderableList.HasList)
            {   
                fvdObject.Properties.reorderableList.onAddCallback += fvdObject.ElementAdded;
                subscribedToEvent = true;
            }

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PropertiesDict);
            bool needsUpdate = EditorGUI.EndChangeCheck();
            if (listDragged && !fvdObject.Properties.reorderableList.IsDragging)
            {
                needsUpdate |= true;
                listDragged = false;
            }
            else if (fvdObject.Properties.reorderableList.IsDragging)
            {
                listDragged = true;
            }
            so.ApplyModifiedProperties();
            if (needsUpdate)
            {
                UpdateOrder();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void UpdateOrder()
        {
            fvdObject.SetNewOrder(fvdObject.Properties.Keys.ToList());
            fvdObject.SaveDefaultsToAsset();
        }
    }
}