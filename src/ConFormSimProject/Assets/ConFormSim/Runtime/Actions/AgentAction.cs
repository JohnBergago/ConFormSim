using System.Collections.Generic;
using UnityEngine;

namespace ConFormSim.Actions
{
    /// <summary>
    /// Provides information whether an action was successful or not. Additionally a
    /// comment can be provided.
    /// </summary>
    public struct ActionOutcome
    {
        /// <summary>
        /// Constructor for an ActionOutcome.
        /// </summary>
        /// <param name="s">Success of the action.</param>
        /// <param name="c">Comment about the outcome.</param>
        public ActionOutcome(bool s, string c="")
        {
            success = s;
            comment = c;
        }

        /// <summary>
        /// Whether the action was successful or not.
        /// </summary>
        public bool success;

        /// <summary>
        /// Add a comment, e.g. a reason for failure.
        /// </summary>
        public string comment;
    }

    /// <summary>
    /// A class inheriting from AgentAction has to implement the Execute method.
    /// This method is called by the AgentActionProvider of an agent.
    /// </summary>
    public class AgentAction : ScriptableObject
    {
        public string actionName;

        /// <summary>
        /// Execute the specified action.
        /// </summary>
        /// <param name="agent">Agent that performs this action.</param>
        /// <param name="kwargs">Optional parameters as a dictionary</param>
        /// <returns>Whether the action was successful or not plus
        /// comment.</returns>
        public virtual ActionOutcome Execute(
            GameObject agent, 
            Dictionary<string, object> kwargs = null)
        {
            // nothing
            return new ActionOutcome(true, "");
        }
    }
}