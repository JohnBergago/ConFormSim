using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using ConFormSim.ObjectProperties;
using ConFormSim.Actions;

public class RewardCollector : MonoBehaviour
{
    public GameObject area;
    public TextMeshProUGUI rewardStatsMonitor;
    private StorageArea m_storageArea;
    private StorageAcademy m_Academy;
    private float m_CurrentReward;
    private float m_PrevReward;
    private float m_CumReward;

    private bool m_Done = false;

    private int m_NumTimePenalty = 0;
    private int m_NumPickCrate = 0;
    private int m_numDeliveredGoal = 0;
    private int m_NumMainGoal = 0;

    public float timePenalty = -0.0005f;
    public float pickCrateReward = 0.05f;
    public float deliveredCrateReward = 0.2f;
    public float mainGoalReward = 0.5f;

    // Debug
    private List<float> m_RewardList = new List<float>();

    // Start is called before the first frame update
    void Awake()
    {
        area = GetComponent<StorageAgent>().area;
        m_storageArea = area.GetComponent<StorageArea>();
        m_Academy = GameObject.FindObjectOfType<StorageAcademy>();
    }

    // Update is called once per Agent step
    public void UpdateReward()
    {
        // apply TimePenalty
        TimePenalty();

        foreach (GameObject crate in m_storageArea.GetInteractableObjects())
        {
            if (crate)
            {
                IMovable movableComponent = crate.GetComponent<IMovable>();
                if (movableComponent != null)
                {
                    // check if any object was newly picked up
                    if (movableComponent.isHolding)
                    {
                        PickedCrate(crate);
                    }
                    CrateInBaseArea(crate);
                }
            }
        }
    }

    public void Reset()
    {
        // Debug.Log("RewardCollector Reset");
        m_CurrentReward = 0;
        m_PrevReward = m_CumReward;
        m_CumReward = 0;
        m_Done = false;
        m_RewardList.Clear();

        UpdateRewardStatsMonitor();
        m_NumTimePenalty = 0;
        m_NumPickCrate = 0;
        m_numDeliveredGoal = 0;
        m_NumMainGoal = 0;
    }

    public bool isDone()
    {
        return m_Done;
    }

    public float GetReward()
    {
        float reward = m_CurrentReward;
        m_CumReward += reward;
        m_CurrentReward = 0;
        return reward;
    }

    public float GetCumulatedReward()
    {
        return m_CumReward;
    }

    public void CrateInBaseArea(GameObject crate)
    {
        GameObject baseArea = m_storageArea.IntersectsBaseArea(crate);
        if (baseArea)
        {
            ObjectPropertyProvider baseOpp = baseArea.GetComponent<ObjectPropertyProvider>();
            ObjectPropertyProvider crateOpp = crate.GetComponent<ObjectPropertyProvider>();
            ObjectController crateContr = crate.GetComponent<ObjectController>();
            IMovable movableComponent = crate.GetComponent<IMovable>();

            ObjectProperty crateTargetTagsProp;
            crateOpp.TryGetObjectProperty("targetTag", out crateTargetTagsProp);
            string[] crateTargetTags = (crateTargetTagsProp as EncodedStringListProperty).stringValues;
            
            ObjectProperty baseTagsProp;
            baseOpp.TryGetObjectProperty("baseTag", out baseTagsProp);
            string[] baseTags = (baseTagsProp as EncodedStringListProperty).stringValues;
            
            if (baseArea 
                && crateTargetTags.Intersect(baseTags).Any()
                && ((!movableComponent.isHolding && m_Academy.boxesNeedDrop)
                    || !m_Academy.boxesNeedDrop)
                && !crateContr.isInCorrectBaseArea)
            {
                crateContr.isInCorrectBaseArea = true;

                // check if result was already given
                if (! crateContr.gotDeliveredReward)
                {
                    // if not give reward
                    if(!m_Academy.sparseRewardOnly)
                    {
                        m_CurrentReward += deliveredCrateReward;
                        m_RewardList.Add(deliveredCrateReward);
                        m_numDeliveredGoal++;
                    }
                    if(m_Academy.taskLevel <= 2)
                    {
                        m_Done = true;
                    }
                    // mark the crate as delivered
                    crateContr.gotDeliveredReward = true;
                }
        
                CheckMainGoal();
                
                if( m_Academy.boxesVanish )
                {
                    DestroyImmediate(crate);
                }
            }
        }
    }

    private void PickedCrate(GameObject crate)
    {
        ObjectController crateContr = crate.GetComponent<ObjectController>();
        if (!crateContr.wasPicked)
        {
            crateContr.wasPicked = true;
            
            if(!m_Academy.sparseRewardOnly)
            {
                m_CurrentReward += pickCrateReward;
                m_RewardList.Add(pickCrateReward);
                m_NumPickCrate++;
            }

            if(m_Academy.taskLevel <= 1)
            {
                m_Done = true;
            }
        }
    }

    public void TimePenalty()
    {
        m_CurrentReward += timePenalty;
        m_RewardList.Add(timePenalty);
        m_NumTimePenalty++;
    }

    public void CollisionPenalty()
    {
        m_CurrentReward += -0.000f;
    }

    public void CheckMainGoal()
    {
        List<GameObject> crates = new List<GameObject>(m_storageArea.GetInteractableObjects());

        bool goalReached = true;
        foreach (GameObject crate in crates)
        {
            if (crate)
            {
                ObjectController crateOC;
                if (crate.TryGetComponent<ObjectController>(out crateOC))
                {
                    goalReached &= crateOC.isInCorrectBaseArea;
                }
            }
        }
        if (goalReached)
        {
            if(!m_Academy.sparseRewardOnly)
            {
                m_CurrentReward += mainGoalReward;
                m_RewardList.Add(mainGoalReward);
                m_NumMainGoal++;
            }
            else
            {
                m_CurrentReward += 1.0f;
                m_RewardList.Add(1.0f);
            }
           
            m_Done = true;
        }
    }

    public void PrintDebug()
    {
        float sum = 0;
        string rewString = "";
        foreach (float r in m_RewardList)
        {
            sum += r;
            if (r > 0.5)
            {
                rewString = rewString + r + ", ";
            }
        }       
    }

    public void  UpdateRewardStatsMonitor()
    {
        if(rewardStatsMonitor)
        {
            rewardStatsMonitor.text = 
            "Prev. Eps. Reward Stats:" 
            + "\n  Pickup   (" + pickCrateReward + "): \t\t" + m_NumPickCrate 
            + "\n  Deliver  (" + deliveredCrateReward + "): \t\t" + m_numDeliveredGoal
            + "\n  Goal     (" + mainGoalReward + "): \t\t" + m_NumMainGoal
            + "\n  TimePen  (" + timePenalty + "): \t" + m_NumTimePenalty;
        }
    }

    public float GetPrevReward()
    {
        return m_PrevReward;
    }
}
