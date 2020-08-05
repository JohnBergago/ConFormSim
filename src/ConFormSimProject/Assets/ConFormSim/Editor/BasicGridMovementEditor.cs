using System.Collections.Generic;
using UnityEditor;
using ConFormSim.Actions;

namespace ConFormSim.Editor
{
    /// <summary>
    /// Custom Editor to disable specifications that are not needed for all actions.
    /// </summary>
    [CustomEditor(typeof(BasicGridMovement))]
    [CanEditMultipleObjects]
    public class BasicGridMovementEditor : UnityEditor.Editor
    {
        SerializedProperty m_ActionNameProp;
        SerializedProperty m_MovementProp;
        SerializedProperty m_ObstacleListProp;
        SerializedProperty m_PickUpOnCollisionProp;
        SerializedProperty m_guidePositionProp;

        void OnEnable()
        {
            m_ActionNameProp = serializedObject.FindProperty("actionName");
            m_MovementProp = serializedObject.FindProperty("movement");
            m_ObstacleListProp = serializedObject.FindProperty("obstacleTags");
            m_PickUpOnCollisionProp = serializedObject.FindProperty("pickUpOnCollision");
            m_guidePositionProp = serializedObject.FindProperty("guidePosition");
        }

        override public void OnInspectorGUI()
        {
            var basicGridMovement = target as BasicGridMovement;

            // get action name
            EditorGUILayout.PropertyField(m_ActionNameProp);
            // get value of movement from editor
            EditorGUILayout.PropertyField(m_MovementProp);
            
            // list of option for which the step size should be enabled
            List<MovementType> stepSizeEnableList = new List<MovementType>() 
            {
                MovementType.GoForward,
                MovementType.GoBackward,
                MovementType.GoLeft,
                MovementType.GoRight
            };

            // hide the step size option if it is not a Go movement
            using (var group = new EditorGUILayout.FadeGroupScope(
                stepSizeEnableList.Contains(basicGridMovement.movement) ? 1 : 0))
            {
                if (group.visible == true)
                {
                    EditorGUI.indentLevel++;
                    basicGridMovement.stepSize = EditorGUILayout.FloatField(
                        "Step Size", basicGridMovement.stepSize);
                    if (basicGridMovement.stepSize < 0)
                    {
                        basicGridMovement.stepSize = 0;
                    }
                    EditorGUILayout.PropertyField(m_ObstacleListProp);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.PropertyField(m_PickUpOnCollisionProp);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_guidePositionProp);
                    EditorGUI.indentLevel--;
                } 
            }

            // list of option for which the step size should be enabled
            List<MovementType> roatationAngleEnableList = new List<MovementType>() 
            {
                MovementType.RotateLeft,
                MovementType.RotateRight,
            };

            // hide the roatation angle option if it is not a rotation movement
            using (var group = new EditorGUILayout.FadeGroupScope(
                roatationAngleEnableList.Contains(basicGridMovement.movement) ? 1 : 0))
            {
                if (group.visible == true)
                {
                    EditorGUI.indentLevel++;
                    basicGridMovement.rotationAngle = EditorGUILayout.Slider(
                        "Rotation Angle", basicGridMovement.rotationAngle, 0, 360.0f);
                    EditorGUILayout.PropertyField(m_ObstacleListProp);
                    EditorGUI.indentLevel--;
                } 
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
