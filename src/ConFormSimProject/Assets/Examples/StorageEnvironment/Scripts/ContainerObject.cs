using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class ContainerObject : MonoBehaviour, IInteractable
{
    [Tooltip("The element that are inside this object. They spawn on interaction")]
    public GameObject[] containedElements;

    [Tooltip("The objects will be destroyed after the interaction.")]
    [SerializeField] private bool destroyAfterInteraction;

    [Tooltip("A tool is needed to open the container.")]
    [SerializeField] private bool needsTool;

    private float spawnRadius = 2.0f;
    private GridBehaviour m_Grid;
    private Transform m_PlaneTransform;

    public void Start()
    {
        m_Grid = transform.parent.FindDeepChild("PathGrid").GetComponent<GridBehaviour>();
        m_PlaneTransform = transform.parent.FindDeepChild("Plane").transform;
    }

    public void Interact(Agent MLagent)
    {
        StorageAgent agent = (StorageAgent) MLagent;
        for (int i = 0; i < containedElements.Length; i++)
        {
            Vector3 spawnPos;
            Bounds bounds = Utility.GetBoundsOfAllDeepChilds(containedElements[i]);
            Quaternion spawnRot = new Quaternion();
            do
            {
                spawnPos = GenerateSpawnPos(bounds.extents.y);
                spawnRot.eulerAngles = new Vector3(0, Random.Range(0, 4) * 90.0f, 0);
            } while (
                Utility.OccupiedPosition(
                    spawnPos,
                    spawnRot,
                    bounds.extents,
                    agent.collisionTags).Length > 0);
            GameObject spawned = Instantiate(containedElements[i], spawnPos, spawnRot);
            spawned.transform.SetParent(agent.transform.parent);

        }

        if (destroyAfterInteraction)
        {
            Destroy(gameObject);
        }
    }

    public bool NeedsTool
    {
        get
        {
            return needsTool;
        }
    }

    private Vector3 GenerateSpawnPos(float yOffset)
    {
        float xMin = transform.position.x - spawnRadius;
        if (xMin < 0)
        {
            xMin = 0;
        }
        float xMax = transform.position.x + spawnRadius;
        if (xMax > m_PlaneTransform.localScale.x * 10)
        {
            xMax = m_PlaneTransform.localScale.x * 10;
        }
        float zMin = transform.position.z - spawnRadius;
        if (zMin < 0)
        {
            zMin = 0;
        }
        float zMax = transform.position.z + spawnRadius;
        if (zMax > m_PlaneTransform.localScale.z * 10)
        {
            zMax = m_PlaneTransform.localScale.z * 10;
        }
        float x = Random.Range(xMin, xMax);
        float z = Random.Range(zMin, zMax);

        Vector3 spawnPos = m_Grid.WorldToGridCoordinates(new Vector3(x, yOffset, z));

        return spawnPos;
    }
}
