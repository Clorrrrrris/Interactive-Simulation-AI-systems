using UnityEngine;
using System.Collections.Generic;

public class CollectibleSpawner : MonoBehaviour
{
    public GameObject collectiblePrefab; 
    public int numberOfCollectibles = 10; // Scattered (randomly, different each time the game is run) are 10 objects
    public Vector3 spawnAreaCenter; 
    public Vector3 spawnAreaSize; 

    void Start()
    {
        SpawnCollectibles();
    }

    void SpawnCollectibles()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogError("Collectible prefab is not assigned in the CollectibleSpawner!");
            return;
        }

        for (int i = 0; i < numberOfCollectibles; i++)
        {
            Vector3 randomPosition = GetRandomPositionInSpawnArea();
            Instantiate(collectiblePrefab, randomPosition, Quaternion.identity, transform);
        }
    }

    Vector3 GetRandomPositionInSpawnArea()
    {
        float x = Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2, spawnAreaCenter.x + spawnAreaSize.x / 2);
        float y = spawnAreaCenter.y; // keep y constant
        float z = Random.Range(spawnAreaCenter.z - spawnAreaSize.z / 2, spawnAreaCenter.z + spawnAreaSize.z / 2);

        return new Vector3(x, y, z);
    }

    // To preview in scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
    }
}