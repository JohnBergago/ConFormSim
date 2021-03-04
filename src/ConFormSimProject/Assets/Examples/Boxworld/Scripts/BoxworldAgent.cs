using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using ConFormSim.Actions;
using ConFormSim.Sensors;
using TMPro;

public class BoxworldAgent : Agent
{
    AgentActionProvider m_AgentActionProvider;
    GameGenerator m_MyGame;



    BoxworldRewardCollector m_RewardCollector;

    // variables for monitor
    public TextMeshProUGUI agentStats;

    private int m_stepCountEpisode;
    private float m_RewardEpisode;
    private int m_PrevEpsSteps;
    private float m_PrevEpsReward;

    // current action for heuristic
    private float m_CurrentAction = 0;

    public override void Initialize()
    {
        m_AgentActionProvider = GetComponent<AgentActionProvider>();
        m_MyGame = transform.parent.gameObject.GetComponent<GameGenerator>();
        m_RewardCollector = GetComponent<BoxworldRewardCollector>();
        m_RewardCollector.myGameGenerator = m_MyGame;

        // set color
        Material newMaterial = Instantiate(gameObject.GetComponent<Renderer>().material);
        newMaterial.SetColor("_Color", m_MyGame.worldSettings.agentColor);
        gameObject.GetComponent<Renderer>().material = newMaterial;
        
        // setup the object property sensor for rendering
        ObjectPropertyCameraSensorComponent opcs = GetComponent<ObjectPropertyCameraSensorComponent>();
        opcs.camera = m_MyGame.transform.Find("TopDownCamera").gameObject.GetComponent<Camera>();
        opcs.debugImg = m_MyGame.obsDebugImage;
        opcs.height = m_MyGame.worldSettings.gridSize + 2;
        opcs.width = opcs.height;
        // set max step according to world settings
        this.MaxStep = m_MyGame.worldSettings.maxNumSteps;

        // set monitor to rewardcollector
        m_RewardCollector.rewardStatsMonitor = m_MyGame.transform.FindDeepChild("PrevRewardStats").gameObject.GetComponent<TextMeshProUGUI>();
        // set monitor for agent
        agentStats = m_MyGame.transform.FindDeepChild("AgentStats").gameObject.GetComponent<TextMeshProUGUI>();
    }

    public override void OnActionReceived(float[] vectorAction)
    {   
        m_AgentActionProvider.PerformDiscreteAction(0, (int) vectorAction[0]);
        
        // first update reward
        m_RewardCollector.UpdateReward();
        // the prepare game for next step
        m_MyGame.UpdateOnAgentStep();

        // collect reward
        float reward = m_RewardCollector.GetReward();
        SetReward(reward);
        if (Mathf.Abs(reward) > 0)
        {
            Debug.Log("step: " + this.StepCount + "\tID: " + GetInstanceID() + "\tReward: " + reward);
        }

        if (m_RewardCollector.isDone())
        {
            EndEpisode();
        }         
    }

    public override void OnEpisodeBegin()
    {
        m_MyGame.ResetGame();
        m_RewardCollector.Reset();
        this.MaxStep = m_MyGame.worldSettings.maxNumSteps;
    }

    private int m_lastKeyPress;
    public override void Heuristic(float[] actionsOut)
    {            
        actionsOut[0] = m_CurrentAction;
        m_CurrentAction = m_AgentActionProvider.GetActionIDByName(0, "No Action");
    }

    private void UpdateAgentStats()
    {
        if (m_stepCountEpisode > this.StepCount)
        {
            // reset the monitor
            m_PrevEpsReward = m_RewardCollector.GetPrevReward();
            m_PrevEpsSteps = m_stepCountEpisode;
            m_RewardEpisode = 0;
            m_stepCountEpisode = this.StepCount;
        }
        else
        {
            m_stepCountEpisode = this.StepCount;
            m_RewardEpisode = GetCumulativeReward();

        }
        agentStats.text = 
            "Eps. Steps: " + m_stepCountEpisode
            + "\nReward: " + m_RewardEpisode.ToString("N2")
            + "\n\nPrev. Eps. Steps: " + m_PrevEpsSteps
            + "\nPrev. Eps. Reward: " + m_PrevEpsReward.ToString("N2");
    }

    void Update()
    {
        UpdateAgentStats();


        if (Input.GetKeyDown(KeyCode.W))
        {
            m_CurrentAction = m_AgentActionProvider.GetActionIDByName(0, "Go Fwd");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            m_CurrentAction = m_AgentActionProvider.GetActionIDByName(0, "Go Bwd");
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            m_CurrentAction = m_AgentActionProvider.GetActionIDByName(0, "Go Left");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            m_CurrentAction = m_AgentActionProvider.GetActionIDByName(0, "Go Right");
        }
    }
}
