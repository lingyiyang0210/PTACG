using System.Collections.Generic;
using UnityEngine;

public class LevelSpawnManager : MonoBehaviour
{
    public static LevelSpawnManager Instance { get; private set; }

    [SerializeField] private List<Transform> spawnPointList;

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < spawnPointList.Count; i++)
        {
            Transform temp = spawnPointList[i];
            int randomIndex = Random.Range(i, spawnPointList.Count);
            spawnPointList[i] = spawnPointList[randomIndex];
            spawnPointList[randomIndex] = temp;
        }
    }

    public Transform GetSpawnPoint(int playerIndex)
    {
        if (spawnPointList == null || spawnPointList.Count == 0)
        {
            Debug.LogWarning("LevelSpawnManager has no spawn points assigned!");
            return transform;
        }

        int index = playerIndex % spawnPointList.Count;
        return spawnPointList[index];
    }
}