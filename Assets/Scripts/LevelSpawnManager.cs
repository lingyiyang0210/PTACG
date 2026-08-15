using System.Collections.Generic;
using UnityEngine;

public class LevelSpawnManager : MonoBehaviour
{
    public static LevelSpawnManager Instance { get; private set; }

    [Header("Spawn Points Priority Levels")]
    [SerializeField] private List<Transform> priority1SpawnPoints;
    [SerializeField] private List<Transform> priority2SpawnPoints;
    [SerializeField] private List<Transform> priority3SpawnPoints;

    private List<Transform> prioritySpawnList;

    private void Awake()
    {
        Instance = this;

        prioritySpawnList = new List<Transform>();

        if (priority1SpawnPoints != null)
        {
            prioritySpawnList.AddRange(priority1SpawnPoints);
        }

        if (priority2SpawnPoints != null)
        {
            prioritySpawnList.AddRange(priority2SpawnPoints);
        }

        if (priority3SpawnPoints != null)
        {
            prioritySpawnList.AddRange(priority3SpawnPoints);
        }
    }

    public Transform GetSpawnPoint(int playerIndex)
    {
        if (prioritySpawnList == null || prioritySpawnList.Count == 0)
        {
            Debug.LogWarning("LevelSpawnManager has no spawn points assigned!");
            return transform;
        }

        int index = playerIndex % prioritySpawnList.Count;
        return prioritySpawnList[index];
    }
}