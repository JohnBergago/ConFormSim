using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using TMPro;
using Unity.MLAgents.Sensors;
using ConFormSim.Actions;
using ConFormSim.Sensors;
using ConFormSim.ObjectProperties;

public class StorageAgent : Agent
{
    public bool observationDebugMsgs = false;
    
    public GameObject area;
    private StorageAcademy m_Academy;
    private StorageArea m_MyArea;
    private GridBehaviour m_PathGrid;

    private GameObject m_Ground;
    private Material m_GroundMaterial;
    private Renderer m_GroundRenderer;

    // for object detection and interaction
    [SerializeField] private Transform cameraTransform = null;
    [SerializeField] private Image cursorImage = null;
    public GameObject toolIndicator = null;
    /// <summary>
    /// The transform to which interactable objects can be attached.
    /// </summary>
    public Transform handTransform = null;
    /// <summary>
    /// The fixed joint of the hand transform, which is relevant in a physics
    /// world. (Automatically retrieved on Start())
    /// </summary>
    private FixedJoint handTransformJoint = null;

    /// <summary>
    /// The ObjectDetector component that checks for objects to interact
    /// with. This has to be attached to the agent.
    /// </summary>
    private IObjectDetector m_ObjectDetector;
    
    /// <summary>
    /// The reward collector used to assign rewards to this agent. Is called
    /// OnActionReceived().
    /// </summary>
    private RewardCollector rewardCollector;

    /// <summary>
    /// The agent collides with objects of these tags.
    /// </summary>
    /// <value></value>
    public List<string> collisionTags = new List<string> {"wall"};
    public ScriptableNoiseFunction objectPropertyNoiseFunc;

    /// <summary>
    /// Variable to block the further actions before the last one was finished or 
    /// a coroutine is still running.
    /// </summary>
    private bool activeAction = false;

    // Monitoring:
    private int m_StepCountTotal = 0;
    private int m_StepCountEpisode = 0;
    private int m_StepCountPrevEpisode = 0;
    private float m_RewardEpisode = 0;
    private float m_RewardPrevEpisode = 0;
    private int m_lastAction = 0;
    private float heuristicActionInput = 0;
    /// <summary>
    /// The monitor objects to display all the statistics UI.
    /// </summary>
    public GameObject statsMonitor;
    private TextMeshProUGUI m_AgentStats;


    private AgentActionProvider aActionProvider;

    public override void Initialize()
    {
        m_Academy = GameObject.FindObjectOfType<StorageAcademy>();
        area = transform.parent.gameObject;
        m_MyArea = area.GetComponent<StorageArea>();
        m_PathGrid = m_MyArea.GetPathGrid();
        m_Ground = transform.parent.FindDeepChild("Plane").gameObject;
        m_GroundRenderer = m_Ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_ObjectDetector = GetComponent<IObjectDetector>();

        aActionProvider = GetComponent<AgentActionProvider>();

        // init members
        if(handTransform != null)
        {
            handTransformJoint = handTransform.GetComponent<FixedJoint>();
        }
        else
        {
            Debug.LogError("No hand transform assigned to agent!.");
        }
        rewardCollector = GetComponent<RewardCollector>();
       
        // Init camera sensors if needed
        var resetProperties = m_Academy.envPropertiesChannel;
        
        // set collision tags to avoid collisions with these objects 
        collisionTags.AddRange(m_Academy.interactableTags); 

        m_Academy.cameraType = (CamTypes) Mathf.FloorToInt(
        resetProperties.GetWithDefault("cameraType", (float) m_Academy.cameraType));
        // Disable all cameras to prevent unnecessary rendering        
        Camera tdCam = m_MyArea
                        .transform
                        .FindDeepChild("TopDownCamera")
                        .GetComponent<Camera>();
        Camera tdfCam = m_MyArea
                        .transform
                        .FindDeepChild("TopDownFollowCamera")
                        .GetComponent<Camera>();
        Camera egoCam = m_MyArea
                        .transform
                        .FindDeepChild("Camera")
                        .GetComponent<Camera>();
        tdCam.enabled = false;
        tdfCam.enabled = false;
        egoCam.enabled = false;

        if (m_Academy.useVisual)
        {
            Debug.Log("Use visual observation");
            CameraSensorComponent csc = gameObject
                .AddComponent<CameraSensorComponent>();
            switch (m_Academy.cameraType)
            {
                default:
                case CamTypes.TopDownCamera:
                    csc.Camera = tdCam;
                    break;
                case CamTypes.TopDownFollowCamera:
                    csc.Camera = tdfCam;
                    tdfCam.GetComponent<TopDownFollowCamera>().InitializePosition();
                    break;
                case CamTypes.Camera:
                    csc.Camera = egoCam;
                    break;
            }
            csc.Width = (int) resetProperties.GetWithDefault("visObsWidth", 84);
            csc.Height = (int) resetProperties.GetWithDefault("visObsHeight", 84);
        }
       

        if(m_Academy.useRayPerception)
        {
            Debug.Log("Use RayPerception");
            RayPerceptionSensorComponent3D rpsc3d = gameObject
                .AddComponent<RayPerceptionSensorComponent3D>();
            rpsc3d.RaysPerDirection = 15;
            rpsc3d.MaxRayDegrees = 180;
            rpsc3d.SphereCastRadius = 0.1f;
            rpsc3d.RayLength = 100;

            // Add detectable tags (walls and all item and base types)
            rpsc3d.DetectableTags = new List<string>();
            rpsc3d.DetectableTags.AddRange(collisionTags);
            rpsc3d.DetectableTags.AddRange(m_Academy.interactableTags);
            rpsc3d.DetectableTags.AddRange(m_Academy.baseTags);
        }

        RawImage debugImg = m_MyArea.transform.FindDeepChild("ObjectPropertyImage").gameObject.GetComponent<UnityEngine.UI.RawImage>();
        debugImg.enabled = false;
        if(m_Academy.useObjectPropertyCamera)
        {
            Debug.Log("Add ObjectProperty Camera sensor");
            ObjectPropertyCameraSensorComponent opcsc = gameObject
                .AddComponent<ObjectPropertyCameraSensorComponent>();
            opcsc.featureVectorDefinition = m_Academy.featureVectorDefinition;
            opcsc.width = (int) resetProperties.GetWithDefault("visObsWidth", 42);
            opcsc.height = (int) resetProperties.GetWithDefault("visObsHeight", 42);
            debugImg.enabled = true;
            opcsc.debugImg = debugImg;
            switch (m_Academy.cameraType)
            {
                default:
                case CamTypes.TopDownCamera:
                    opcsc.camera = tdCam;
                    break;
                case CamTypes.TopDownFollowCamera:
                    opcsc.camera = tdfCam;
                    tdfCam.GetComponent<TopDownFollowCamera>().InitializePosition();
                    break;
                case CamTypes.Camera:
                    opcsc.camera = egoCam;
                    break;
            }
            opcsc.noiseFunction = objectPropertyNoiseFunc;
        }

        if (!m_Academy.useVisual && !m_Academy.useRayPerception && !m_Academy.useObjectPropertyCamera)
        {
            Debug.Log("Use State Observation");
            StorageStateVectorSensorComponent ssvsc = gameObject
                .AddComponent<StorageStateVectorSensorComponent>();
            ssvsc.academy = m_Academy;
            ssvsc.agent = this.gameObject;
            ssvsc.area = m_MyArea;
            // add reference feature vector definition
            ssvsc.featureVectorDefinition = m_Academy.featureVectorDefinition;
        }

        // init property
        ObjectPropertyProvider opp = GetComponent<ObjectPropertyProvider>();
        opp.AvailableProperties = m_Academy.featureVectorDefinition;
        BoolObjectProperty isAgentProp = ScriptableObject.CreateInstance<BoolObjectProperty>();
        isAgentProp.value = true;
        opp.SetObjectProperty("isAgent", isAgentProp);

        // Init monitoring Objects
        InitMonitoring();
        Debug.Log("Init Agent");
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    public override void OnActionReceived(float[] vectorAction)
    {   
        GameObject interactableObj = null;
        // first check if there is currently a object the agent is carrying
        if (handTransform.childCount > 0)
        {
            // check non physics transform
            interactableObj = handTransform.GetChild(0).gameObject;
        }
        else if (handTransformJoint != null)
        {
            // check physical indicator of carrying things
            interactableObj = handTransformJoint.connectedBody.gameObject;
        }
        else
        {
            // see if there are detected objects
            interactableObj = m_ObjectDetector.DetectedObject;
        }

        aActionProvider.PerformDiscreteAction(
            0,
            (int) vectorAction[0], 
            kwargs: new Dictionary<string, object>{
                {"interactable_gameobject", (object) interactableObj},
                {"guide_transform", (object) handTransform}});

        m_lastAction = (int) vectorAction[0];

        rewardCollector.UpdateReward();

        float reward = rewardCollector.GetReward();
        SetReward(reward);
        if(Mathf.Abs(reward) > 0.01f)
        {
            Debug.Log("step: " + this.StepCount + "\tID: " + GetInstanceID() + "\tReward: " + reward);
        }
        
        if (rewardCollector.isDone())
        {
            if(m_Academy.goalReachedMaterial)
            {
                StartCoroutine(GoalScoredSwapGroundMaterial(m_Academy.goalReachedMaterial, Time.fixedDeltaTime));
            }
            EndEpisode();
        }
        if(m_MyArea.GetInteractableObjects().Length == 0)
        {
            EndEpisode();
        }
        m_StepCountTotal++;
    }


    public override void OnEpisodeBegin()
    {
        Debug.Log("Reset Agent");
        m_MyArea.ResetArea();

        // update maxStep based on latest side channel messages
        MaxStep = m_Academy.maxStep;

        m_MyArea.ResetAgentPosition();
        // Init Follow Cam Position
        m_MyArea.transform.Find("TopDownFollowCamera")
                    .GetComponent<TopDownFollowCamera>()
                    .InitializePosition();

        // set collision tags to avoid collisions with these objects 
        collisionTags.Clear();
        collisionTags.Add("wall");
        collisionTags.AddRange(m_Academy.interactableTags); 
        // set interactable tags to be detected by the object detector
        m_ObjectDetector.DetectableTags = m_Academy.interactableTags.ToList();
        // for scripted ai  
        m_PathGrid = m_MyArea.GetPathGrid();
        m_PathGrid.UpdateGrid();

        GetComponent<RewardCollector>().Reset();
    }

    void Update()
    {
        // get Input for heuristics
        if (Input.GetKeyDown(KeyCode.W))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Go Fwd");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Go Bwd");
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Go Left");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Go Right");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Rotate Left");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Rotate Right");
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Interact");
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            heuristicActionInput = aActionProvider.GetActionIDByName(0, "Pick Up");
        }
        // Update UI
        MonitorUpdate();
    }
    // On every Update check for reachable object in front of the agent
    // void FixedUpdate()
    // {
    //     // basically here we just set up the crosshair to follow the detected object
    //     // does the agent already carry any interactable?
    //     if (handTransform.childCount > 0)
    //     {
    //         // check for non physics case
    //         // set the cursor position on that object
    //         if (cursorImage)
    //         {
    //             cursorImage.transform.localPosition = WorldToCanvasPosition(
    //                 cursorImage.canvas, 
    //                 cameraTransform.GetComponent<Camera>(), 
    //                 handTransform.transform.GetChild(0).position);
    //         }
    //     }
    //     else if (handTransform.GetComponent<FixedJoint>() != null)
    //     {
    //         if (cursorImage)
    //         {
    //             cursorImage.transform.localPosition = WorldToCanvasPosition(
    //                 cursorImage.canvas, 
    //                 cameraTransform.GetComponent<Camera>(), 
    //                 handTransform.GetComponent<FixedJoint>()
    //                     .connectedBody
    //                     .gameObject
    //                     .transform
    //                     .position);
    //         }
    //     }
    //     else
    //     {
    //         if (GetComponent<IObjectDetector>().HasObjectDetected)
    //         {
    //             if (cursorImage)
    //             {
    //                 cursorImage.gameObject.SetActive(true);
    //                 cursorImage.transform.localPosition = WorldToCanvasPosition(
    //                     cursorImage.canvas, 
    //                     cameraTransform.GetComponent<Camera>(), 
    //                     m_ObjectDetector.DetectedObject.transform.position);
    //                 cursorImage.color = Color.green;
    //             }
    //         }
    //         else
    //         {
    //             if(cursorImage)
    //             {
    //                 cursorImage.color = Color.white;
    //                 cursorImage.gameObject.SetActive(false);
    //             }
    //         }
    //     }
    // }
    
    /// <summary> 
    /// Calculates a position on the canvas based on a position in world coordinates.
    /// This is useful for UI Elements that are intended to track a GameObject (i.e.
    /// healthbars).
    /// </summary>
    Vector2 WorldToCanvasPosition(Canvas canvas, Camera camera, Vector3 position)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 viewPoint = camera.WorldToViewportPoint(position);
        Vector2 screenPoint = canvas.worldCamera.ViewportToScreenPoint(viewPoint);
        Vector2 result;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPoint, 
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, 
            out result);
        return result; 
    }

    /// <summary>
    /// Coroutine to rotate a given transform to a desired rotation within the given
    /// duration time.
    /// </summary>
    /// <param name="target"> The target Transform to be rotated. </param>
    /// <param name="desiredRotation"> Desired rotation as Quaternion. </param>
    /// <param name="duration"> Time span in which the rotation will be finished. </param>
    IEnumerator RotateTo(Transform target, Quaternion desiredRotation, float duration)
    {
        activeAction = true;
        float t = 0f;
        Quaternion start = target.rotation;
        while(t < duration)
        {
            target.rotation = Quaternion.Slerp(start, desiredRotation, t / duration);
            yield return null;
            t += Time.deltaTime;
        }
        target.rotation = desiredRotation; 
        activeAction = false;
    }

    private int m_lastKeyPress;
    public override void Heuristic(float[] actionsOut)
    {        
        actionsOut[0] = heuristicActionInput;
        heuristicActionInput = aActionProvider.GetActionIDByName(0, "No Action");
    }

    private void MonitorUpdate()
    {
        // was there a reset?
        if(m_StepCountEpisode > this.StepCount)
        {
            m_RewardPrevEpisode = GetComponent<RewardCollector>().GetPrevReward();
            m_StepCountPrevEpisode = m_StepCountEpisode;
            m_RewardEpisode = 0;
            m_StepCountEpisode = this.StepCount;
        }
        else
        {
            m_StepCountEpisode = this.StepCount;
            m_RewardEpisode = GetCumulativeReward();
        }        

        m_AgentStats.text = 
            "Tot. Agent Steps: " + m_StepCountTotal + "\n\n"
            + "Action: " + GetComponent<AgentActionProvider>().GetActionName(0, m_lastAction) + "\n"
            + "Eps. Steps: " + m_StepCountEpisode + "\n"
            + "Eps. Reward: " +  m_RewardEpisode.ToString("N4") + "\n\n"
            + "Prev. Eps. Steps: " + m_StepCountPrevEpisode + "\n"
            + "Prev. Eps. Reward: " + m_RewardPrevEpisode.ToString("N4") + "\n";
    }

    private void InitMonitoring()
    {
        GameObject[] monitors = GameObject.FindGameObjectsWithTag("monitor");
        foreach(GameObject monitor in monitors)
        {
            if (monitor.transform.parent == area.transform)
            {
                statsMonitor = monitor;
                break;
            }
        }

        GameObject[] agentStats = GameObject.FindGameObjectsWithTag("monitorAgentStats");
        Debug.Log("AgentStatsLength: " + agentStats.Length);
        foreach(GameObject agentStat in agentStats)
        {
            if (agentStat.transform.parent.parent == area.transform)
            {
                m_AgentStats = agentStat.GetComponent<TextMeshProUGUI>();
                break;
            }
        }

        // deactivate stats monitor until academy enables it
        statsMonitor.SetActive(false);
    }

}
