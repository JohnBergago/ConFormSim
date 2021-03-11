using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

namespace ConFormSim.Actions
{
    [System.Serializable]
    public class AgentActionBranch
    {
        public List<DiscreteAgentAction> actionList;
    }
    /// <summary>
    /// An AgentActionProvider is a component of an agent. The agent's script
    /// accesses it to perform the right action. That way an agent script can be
    /// reused for different sets of actions.
    /// </summary>
    [RequireComponent(typeof(Agent))]
    public class AgentActionProvider : ActuatorComponent
    {
        ActionSpec m_ActionSpec;
        /// <summary>
        /// List of all discrete actions that are available for an agent. Can be
        /// set in editor by dragging scripts of type AgentAction into the
        /// inspector. 
        /// </summary>
        public List<AgentActionBranch> discreteActionBranches;

        /// <summary>
        /// List of all continuous actions that are available on the agent. Can
        /// be set in editor by dragging scripts of type AgentAction into the
        /// inspector.
        /// </summary>
        public List<ContinuousAgentAction> continuousActions;

         /// <summary>
        /// Creates a BasicActuator.
        /// </summary>
        /// <returns></returns>
#pragma warning disable 672
        public override IActuator CreateActuator()
#pragma warning restore 672
        {
            return new AgentActionProviderActuator(GetComponent<Agent>(), ActionSpec);
        }

        public override ActionSpec ActionSpec
        {
            get 
            { 
                // Remove all elements from list, that are null
                discreteActionBranches.RemoveAll(item => item == null);
                continuousActions.RemoveAll(item => item == null);
                // calculate discrete branch sizes
                int[] discreteBranchSizes = new int[discreteActionBranches.Count];
                for (int i = 0; i < discreteActionBranches.Count; i++)
                {
                    discreteBranchSizes[i] = discreteActionBranches[i].actionList.Count;
                }

                return new ActionSpec(continuousActions.Count, discreteBranchSizes);
            }
        }

        /// <summary>
        /// Perform a discrete action listed in the agent action provider.
        /// </summary>
        /// <param name="branchIdx">Index of the selected action branch.</param>
        /// <param name="actionIdx">Index of the action within the selected action branch.</param>
        /// <param name="kwargs">Additional arguments which are passed to the action on execution.</param>
        public void PerformDiscreteAction(
            int branchIdx,
            int actionIdx,
            Dictionary<string, object> kwargs = null)
        {
            if (branchIdx < discreteActionBranches.Count && branchIdx >= 0 && discreteActionBranches[branchIdx] != null)
            {
                AgentActionBranch aab = discreteActionBranches[branchIdx];
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

        /// <summary>
        /// Perform a continuous action listed in the agent action provider.
        /// </summary>
        /// <param name="actionIdx">Index of the selected action.</param>
        /// <param name="value">Value of the action. Usually between -1 and 1.</param>
        /// <param name="kwargs">Additional arguments which are passed to the action on execution.</param>
        public void PerformContinuousAction(
            int actionIdx,
            float actionValue,
            Dictionary<string, object> kwargs = null)
        {
            if (actionIdx < continuousActions.Count && actionIdx >= 0 && continuousActions[actionIdx] != null)
            {

                    if (kwargs == null)
                    {
                        kwargs = new Dictionary<string, object>();
                    }
                    ActionOutcome o = continuousActions[actionIdx].Execute(gameObject, actionValue, kwargs);
            }
            else 
            {
                Debug.LogError("The continuous action index " + actionIdx + " is out of" 
                    + " bounds of the action list. Did you add all necessary actions"
                    + " to the AgentActionProvider?");
                throw new System.Exception("Continuous action index out of bounds of "
                    + "continuouActions List. Make sure to add all necessary actions to the " 
                    + "AgentActionProvider.");
            }
        }

        public void PerformActions(ActionBuffers actionBuffers, Dictionary<string, object> kwargs = null)
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
        public string GetDiscreteActionName(int branchIdx, int actionIdx)
        {
            if (branchIdx < discreteActionBranches.Count && branchIdx >= 0 && discreteActionBranches[branchIdx] != null)
            {
                AgentActionBranch aab = discreteActionBranches[branchIdx];
                if (actionIdx < aab.actionList.Count && actionIdx >=0 && aab.actionList[actionIdx])
                {
                    return aab.actionList[actionIdx].actionName;
                }
            }
        
            Debug.LogError("The action index " + actionIdx + "is out of bounds " 
                    + "of the action list. Did you add all necessary actions to the "
                    + "AgentActionProvider?");
            return "Invalid Index";
        }

        /// <summary>
        /// Retrieve the ID of an action by its name.
        /// </summary>
        /// <param name="branchIdx">Index of the action branch.</param>
        /// <param name="actionName">Name of the action in question.</param>
        /// <returns>ID of the action with actionName.</returns>
        public int GetDiscreteActionIDByName(int branchIdx, string actionName)
        {
            if (branchIdx < discreteActionBranches.Count)
            {
                AgentActionBranch aab = discreteActionBranches[branchIdx];
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

    /// <summary>
    /// An ActuatorImplementation following the example of the BuiltIn MLAgents
    /// VectorActuator, to forward to provided IActionReceivers.
    /// </summary>
    public class AgentActionProviderActuator : IActuator, IHeuristicProvider
    {
        IActionReceiver m_ActionReceiver;
        IHeuristicProvider m_HeuristicProvider;
        ActionBuffers m_ActionBuffers;
        internal ActionBuffers ActionBuffers
        {
            get => m_ActionBuffers;
            private set => m_ActionBuffers = value;
        }

        /// <summary>
        /// Create a AgentActionProviderActuator that forwards to the provided IActionReceiver.
        /// </summary>
        /// <param name="actionReceiver">The <see cref="IActionReceiver"/> used for OnActionReceived and WriteDiscreteActionMask.
        /// If this parameter also implements <see cref="IHeuristicProvider"/> it will be cast and used to forward calls to
        /// <see cref="IHeuristicProvider.Heuristic"/>.</param>
        /// <param name="actionSpec"></param>
        /// <param name="name"></param>
        public AgentActionProviderActuator(IActionReceiver actionReceiver,
                              ActionSpec actionSpec,
                              string name = "AgentActionProviderActuator")
            : this(actionReceiver, actionReceiver as IHeuristicProvider, actionSpec, name) { }

         /// <summary>
        /// Create a VectorActuator that forwards to the provided IActionReceiver.
        /// </summary>
        /// <param name="actionReceiver">The <see cref="IActionReceiver"/> used for OnActionReceived and WriteDiscreteActionMask.</param>
        /// <param name="heuristicProvider">The <see cref="IHeuristicProvider"/> used to fill the <see cref="ActionBuffers"/>
        /// for Heuristic Policies.</param>
        /// <param name="actionSpec"></param>
        /// <param name="name"></param>
        public AgentActionProviderActuator(IActionReceiver actionReceiver,
                              IHeuristicProvider heuristicProvider,
                              ActionSpec actionSpec,
                              string name = "AgentActionProviderActuator")
        {
            m_ActionReceiver = actionReceiver;
            m_HeuristicProvider = heuristicProvider;
            ActionSpec = actionSpec;
            string suffix;
            if (actionSpec.NumContinuousActions == 0)
            {
                suffix = "-Discrete";
            }
            else if (actionSpec.NumDiscreteActions == 0)
            {
                suffix = "-Continuous";
            }
            else
            {
                suffix = $"-Continuous-{actionSpec.NumContinuousActions}-Discrete-{actionSpec.NumDiscreteActions}";
            }
            Name = name + suffix;
        }

        /// <inheritdoc />
        public void ResetData()
        {
            m_ActionBuffers = ActionBuffers.Empty;
        }

        /// <inheritdoc />
        public void OnActionReceived(ActionBuffers actionBuffers)
        {
            m_ActionBuffers = actionBuffers;
            m_ActionReceiver.OnActionReceived(m_ActionBuffers);
        }

        public void Heuristic(in ActionBuffers actionBuffersOut)
        {
            m_HeuristicProvider?.Heuristic(actionBuffersOut);
        }

        /// <inheritdoc />
        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            m_ActionReceiver.WriteDiscreteActionMask(actionMask);
        }

        /// <inheritdoc/>
        public ActionSpec ActionSpec { get; }

        /// <inheritdoc />
        public string Name { get; }
    }
}