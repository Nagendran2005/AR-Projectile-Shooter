using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Header("Three Target Prefabs")]
    public GameObject[] targetPrefabs;

    [Header("Spawn")]
    public float spawnHeight = 1.5f;
    public float spacing = 0.8f;

    private readonly List<GameObject> spawnedTargets = new();

    public void SpawnTargets(Vector3 planePosition)
    {
        if (spawnedTargets.Count > 0)
            return;

        if (targetPrefabs.Length < 3)
        {
            Debug.LogError("Assign 3 Target Prefabs.");
            return;
        }

        float startX = -spacing;

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = planePosition;

            spawnPos += Vector3.right * (startX + spacing * i);

            spawnPos.y += spawnHeight;

            GameObject target = Instantiate(
                targetPrefabs[i],
                spawnPos,
                Quaternion.identity);

            spawnedTargets.Add(target);
        }
    }

    public int RemainingTargets()
    {
        int count = 0;

        foreach (GameObject obj in spawnedTargets)
        {
            if (obj != null)
                count++;
        }

        return count;
    }
}