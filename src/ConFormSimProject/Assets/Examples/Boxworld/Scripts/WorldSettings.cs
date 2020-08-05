using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="BoxworldSettings", menuName="Boxworld/Settings")]
public class WorldSettings : ScriptableObject
{
    /// <summary>
    /// Number of training areas to spawn
    /// </summary>
    public int numAreas = 1;

    /// <summary>
    /// Height and width of the grid
    /// </summary>
    public int gridSize = 12;

    /// <summary>
    /// Number of boxes in the path to the goal. The actual length will be
    /// sampled from this array.
    /// </summary>
    /// <value></value>
    public int[] solutionLength = new int[] {1, 2, 3, 4};

    /// <summary>
    /// Possible values for number of forward distractors
    /// </summary>
    /// <value></value>
    public int[] numForward = new int[] {0, 1, 2, 3, 4};

    /// <summary>
    /// Possible values for number of backward distractors
    /// </summary>
    /// <value></value>
    public int[] numBackward = new int[] {0};

    /// <summary>
    /// Lengtn of forward distractor branches
    /// </summary>
    public int branchLength = 1;

    /// <summary>
    /// Number of steps before the episode is halted.
    /// </summary>
    public int maxNumSteps = 120;

    /// <summary>
    /// Random number of generator state.
    /// </summary>
    public int randomState;

    public GameObject keyPrefab;
    public GameObject lockPrefab;
    public GameObject gemPrefab;
    public GameObject agentPrefab;

    /// <summary>
    /// Color Pool for objects.
    /// </summary>
    /// <value></value>
    public Color[] colors = new Color[]{
        new Color(0.700f, 0.350f, 0.350f), new Color(0.700f, 0.454f, 0.350f), new Color(0.700f, 0.559f, 0.350f), new Color(0.700f, 0.664f, 0.350f),
        new Color(0.629f, 0.700f, 0.350f), new Color(0.524f, 0.700f, 0.350f), new Color(0.420f, 0.700f, 0.350f), new Color(0.350f, 0.700f, 0.384f),
        new Color(0.350f, 0.700f, 0.490f), new Color(0.350f, 0.700f, 0.595f), new Color(0.350f, 0.700f, 0.700f), new Color(0.350f, 0.594f, 0.700f),
        new Color(0.350f, 0.490f, 0.700f), new Color(0.350f, 0.384f, 0.700f), new Color(0.419f, 0.350f, 0.700f), new Color(0.524f, 0.350f, 0.700f),
        new Color(0.630f, 0.350f, 0.700f), new Color(0.700f, 0.350f, 0.665f), new Color(0.700f, 0.350f, 0.559f), new Color(0.700f, 0.350f, 0.455f)
    };

    public Color gemColor = new Color(1, 1, 1);

    public Color agentColor = new Color(0.5f, 0.5f, 0.5f);

    public int MaxNumKeys 
    {
        get { return colors.Length; }
    }
}
