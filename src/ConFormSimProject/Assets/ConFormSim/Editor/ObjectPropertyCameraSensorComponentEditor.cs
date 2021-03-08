using UnityEditor;
using Unity.MLAgents.Editor;
using ConFormSim.Sensors;

namespace ConFormSim.Editor
{
    [CustomEditor(typeof(ObjectPropertyCameraSensorComponent))]
    [CanEditMultipleObjects]
    internal class ObjectPropertyCameraSensorComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            // Drawing the CameraSensorComponent
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty("m_Camera"), true);
            EditorGUILayout.PropertyField(so.FindProperty("debugImg"), true);
            EditorGUI.BeginDisabledGroup(!EditorUtilities.CanUpdateModelProperties());
            {
                // These fields affect the sensor order or observation size,
                // So can't be changed at runtime.
                EditorGUILayout.PropertyField(so.FindProperty("featureVectorDefinition"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Width"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Height"), true);
            }
            EditorGUI.EndDisabledGroup();

            var requireSensorUpdate = EditorGUI.EndChangeCheck();
            so.ApplyModifiedProperties();

            if (requireSensorUpdate)
            {
                UpdateSensor();
            }
        }

        void UpdateSensor()
        {
            var sensorComponent = serializedObject.targetObject as ObjectPropertyCameraSensorComponent;
            sensorComponent?.UpdateSensor();
        }
    }
}