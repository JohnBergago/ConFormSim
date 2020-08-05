using UnityEditor;
using UnityEngine;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using ConFormSim.Actions;

namespace ConFormSim.Editor
{
    [CustomEditor(typeof(AgentActionProvider))]
    [CanEditMultipleObjects]
    internal class AgentActionProviderEditor : UnityEditor.Editor
    {
        AgentActionProvider aap;
        GameObject agent;

        BehaviorParameters agentBehaviorParam;

        void OnEnable()
        {
            aap = (AgentActionProvider) target;
            agent = aap.gameObject;
            agentBehaviorParam = aap.GetComponent<BehaviorParameters>();
        }
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();
            // Set Action BrainParameters according to the AgentActionProvider
            if (agentBehaviorParam.BrainParameters.VectorActionSpaceType == SpaceType.Continuous)
            {
                
                if (aap.actionBranches == null)
                {
                    aap.actionBranches = new List<AgentActionBranch>{new AgentActionBranch()};
                }
                
                if (aap.actionBranches.Count > 0)
                {
                    agentBehaviorParam.BrainParameters.VectorActionSize[0] = aap.actionBranches[0].actionList.Count;
                }
                if (aap.actionBranches.Count > 1)
                {
                    aap.actionBranches.RemoveRange(1, aap.actionBranches.Count - 1);
                }
                // for continuous actions there is only one branch
                agentBehaviorParam.BrainParameters.VectorActionSize = new int[1];
            }
            else
            {
                if(aap.actionBranches == null)
                {
                    aap.actionBranches = new List<AgentActionBranch>();
                }
                // for discrete Actions there can be multiple branches
                agentBehaviorParam.BrainParameters.VectorActionSize = new int[aap.actionBranches.Count];
                for ( int i = 0; i < aap.actionBranches.Count; i++)
                {
                    agentBehaviorParam.BrainParameters.VectorActionSize[i] = 
                        aap.actionBranches[i].actionList.Count;
                }
            }
            

            EditorGUILayout.PropertyField(so.FindProperty("actionBranches"), true); 
            serializedObject.ApplyModifiedProperties();
        }
    }
}