using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    [System.Serializable]
    public class AgentActionBranch
    {
        public List<AgentAction> actionList;
    }
    /// <summary>
    /// An AgentActionProvider is a component of an agent. The agent's script
    /// accesses it to perform the right action. That way an agent script can be
    /// reused for different sets of actions.
    /// </summary>
    public class AgentActionProvider : MonoBehaviour
    {
        /// <summary>
        /// List of all actions that are available for an agent. Can be set in
        /// editor by dragging scripts of type AgentAction into the inspector. 
        /// </summary>
        public List<AgentActionBranch> actionBranches;

        void Start()
        {
            // Remove all elements from list, that are null
            actionBranches.RemoveAll(item => item == null);
        }

        public void PerformDiscreteAction(
            int branchIdx,
            int actionIdx,
            Dictionary<string, object> kwargs = null)
        {
            if (branchIdx < actionBranches.Count && branchIdx >= 0 && actionBranches[branchIdx] != null)
            {
                AgentActionBranch aab = actionBranches[branchIdx];
                if (actionIdx < aab.actionList.Count && actionIdx >=0 && aab.actionList[actionIdx])
                {
                    if (kwargs == null)
                    {
                        kwargs = new Dictionary<string, object>();
                    }
                    ActionOutcome o = aab.actionList[actionIdx].Execute(gameObject, kwargs);
                    // if (aab.actionList[actionIdx].name != "None")
                    //     Debug.Log("Action: " + aab.actionList[actionIdx].name + " Success: " + o.success + " comment: " + o.comment);
                }
            }
            else 
            {
                Debug.LogError("The action branch index " + branchIdx + " is out of" 
                    + " bounds of the branch list. Did you add all necessary action"
                    + " branches to the AgentActionProvider?");
                throw new System.Exception("action branch index out of bounds of "
                    + "actionBranches. Make sure to add all necessary actions to the " 
                    + "AgentActionProvider.");
            }
        }

        public void PerformActions(float[] vectorAction, params object[] args)
        {

            // TODO map each value in array to an action from list --> pass value to
            // designated action.

            Debug.LogError("Not implemented yet.");
        }
        
        /// <summary>
        /// Get the name of the action called.
        /// </summary>
        /// <param name="actionIdx">ID to retrieve the name from.</param>
        /// <returns></returns>
        public string GetActionName(int branchIdx, int actionIdx)
        {
            if (branchIdx < actionBranches.Count && branchIdx >= 0 && actionBranches[branchIdx] != null)
            {
                AgentActionBranch aab = actionBranches[branchIdx];
                if (actionIdx < aab.actionList.Count && actionIdx >=0 && aab.actionList[actionIdx])
                {
                    return aab.actionList[actionIdx].actionName;
                }
            }
        
            Debug.LogError("The action index " + actionIdx + "is out of bounds " 
                    + "of the action list. Did you add all necessary actions to the"
                    + "AgentActionProvider?");
            return "Invalid Index";
        }

        /// <summary>
        /// Retrieve the ID of an action by its name.
        /// </summary>
        /// <param name="actionName">Name of the action in question.</param>
        /// <returns>ID of the action with actionName.</returns>
        public int GetActionIDByName(int branchIdx, string actionName)
        {
            if (branchIdx < actionBranches.Count)
            {
                AgentActionBranch aab = actionBranches[branchIdx];
                for (int i = 0; i < aab.actionList.Count; i++)
                {
                    if (aab.actionList[i] && aab.actionList[i].actionName == actionName)
                    {
                        return i;
                    }
                }
            }
            Debug.LogError("Couldn't find any action with name " + actionName +
                " in branch " + branchIdx + ".");
            // return -1 if no action found with that name
            return -1;
        }
    }
}