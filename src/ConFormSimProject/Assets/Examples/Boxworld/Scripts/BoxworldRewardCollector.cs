using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoxworldRewardCollector : MonoBehaviour
{
    public float stepReward;
    public float openCorrectReward;
    public float openWrongReward;
    public float goalReward;

    [HideInInspector]
    public GameGenerator myGameGenerator;

    private float m_CurrentReward;
    private float m_CumReward;
    private bool m_Done;
    
    // counting variables for statistics and reward monitor
    public TextMeshProUGUI rewardStatsMonitor;
    int m_NumOpenCorrect;
    int m_NumOpenWrong;
    int m_NumGoal;
    int m_NumSteps;
    float m_PrevReward;

    public void UpdateReward()
    {
        // apply reward per step
        m_CurrentReward += stepReward;
        m_NumSteps++;

        int agentPosX = Mathf.RoundToInt(transform.localPosition.x);
        int agentPosY = Mathf.RoundToInt(transform.localPosition.z);

        // check if a distractor was hit
        if (myGameGenerator.PositionIsDistractor(agentPosX, agentPosY))
        {
            m_CurrentReward += openWrongReward;
            m_NumOpenWrong ++;
            m_Done = true;
        }
        // else if it is a correct key
        else if(myGameGenerator.PositionIsKey(agentPosX, agentPosY)) 
        {
            m_CurrentReward += openCorrectReward;
            m_NumOpenCorrect++;
        }
        else if(myGameGenerator.PositionIsGem(agentPosX, agentPosY))
        {
            m_CurrentReward += goalReward;
            m_NumGoal++;
            m_Done = true;
        }
    }

    public void Reset()
    {
        m_PrevReward = m_CumReward;

        Debug.Log("RewardCollector Reset");
        m_CurrentReward = 0;
        m_CumReward = 0;
        m_Done = false;
        m_CurrentReward = 0f;

        // reset monitor variables
        UpdateRewardStatsMonitor();
        m_NumOpenCorrect = 0;
        m_NumOpenWrong = 0;
        m_NumGoal = 0;
        m_NumSteps = 0;
    }

    public float GetReward()
    {
        float reward = m_CurrentReward;
        m_CumReward += reward;
        m_CurrentReward = 0;
        return reward;
    }

    public bool isDone()
    {
        return m_Done;
    }

    public void  UpdateRewardStatsMonitor()
    {
        if(rewardStatsMonitor)
        {
            rewardStatsMonitor.text = 
            "Prev. Eps. Reward Stats:" 
            + "\n  Open Correct  (" + openCorrectReward + "): " + m_NumOpenCorrect 
            + "\n  Open Wrong (" + openWrongReward + "):  " + m_NumOpenWrong
            + "\n  Goal \t(" + goalReward + "): \t  " + m_NumGoal
            + "\n  Step \t(" + stepReward + "): \t  " + m_NumSteps;
        }
    }

    public float GetPrevReward()
    {
        return m_PrevReward;
    }
}
