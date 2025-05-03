using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Random = UnityEngine.Random;
using System.Linq;

public class AmmoBoxSpawner : NetworkBehaviour
{
    [Header("Ammo Box Prefabs")]
    public List<GameObject> ammoBoxPrefabs; // List ของ Ammo Box Prefab ทั้ง 3 ประเภท

    [Header("Spawn Settings")]
    public List<Transform> spawnPoints;
    public float spawnInterval = 10f;
    public bool spawnAllTypes = true;
    public float spawnRadius = 2f;
    public int maxSpawnAttempts = 10;

    private float nextSpawnTime;
    private List<Vector3> spawnedPositions = new List<Vector3>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        nextSpawnTime = Time.time + spawnInterval;
        spawnedPositions.Clear();
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time >= nextSpawnTime)
        {
            if (spawnAllTypes)
            {
                SpawnAllAmmoBoxes();
            }
            else
            {
                SpawnAmmoBox();
            }

            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    Vector3 GetRandomSpawnPosition(Transform center)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPosition = center.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            bool overlapping = false;
            foreach (Vector3 pos in spawnedPositions)
            {
                if (Vector3.Distance(randomPosition, pos) < 0.5f)
                {
                    overlapping = true;
                    break;
                }
            }

            if (!overlapping)
            {
                return randomPosition;
            }
        }

        return center.position;
    }

    void SpawnAllAmmoBoxes()
    {
        if (ammoBoxPrefabs == null || ammoBoxPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("โปรดกำหนด Ammo Box Prefabs และ Spawn Points ให้ครบ!");
            return;
        }

        // 🔄 รีเฟรชทุกครั้งก่อน spawn
        List<int> availableSpawnPoints = Enumerable.Range(0, spawnPoints.Count).ToList();
        availableSpawnPoints.Shuffle();
        spawnedPositions.Clear();

        int boxesToSpawn = Mathf.Min(ammoBoxPrefabs.Count, availableSpawnPoints.Count);

        for (int i = 0; i < boxesToSpawn; i++)
        {
            GameObject ammoBoxPrefab = ammoBoxPrefabs[i];
            int spawnPointIndex = availableSpawnPoints[i];
            Transform spawnPoint = spawnPoints[spawnPointIndex];

            Vector3 spawnPosition = GetRandomSpawnPosition(spawnPoint);

            GameObject spawnedAmmoBox = Instantiate(ammoBoxPrefab, spawnPosition, Quaternion.identity);
            if (spawnedAmmoBox.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.Spawn();
                Debug.Log($"Server: Spawned Ammo Box of type: {ammoBoxPrefab.name} at {spawnPosition}");
                spawnedPositions.Add(spawnPosition);
            }
            else
            {
                Debug.LogError($"Ammo Box Prefab {ammoBoxPrefab.name} does not have a NetworkObject component!");
                Destroy(spawnedAmmoBox);
            }
        }
    }

    void SpawnAmmoBox()
    {
        if (ammoBoxPrefabs == null || ammoBoxPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("โปรดกำหนด Ammo Box Prefabs และ Spawn Points ให้ครบ!");
            return;
        }

        float[] spawnProbabilities = new float[ammoBoxPrefabs.Count];
        float totalProbability = 0f;

        for (int i = 0; i < ammoBoxPrefabs.Count; i++)
        {
            spawnProbabilities[i] = ammoBoxPrefabs.Count - i;
            totalProbability += spawnProbabilities[i];
        }

        float randomValue = Random.Range(0f, totalProbability);
        GameObject selectedAmmoBoxPrefab = null;
        float cumulativeProbability = 0f;

        for (int i = 0; i < ammoBoxPrefabs.Count; i++)
        {
            cumulativeProbability += spawnProbabilities[i];
            if (randomValue <= cumulativeProbability)
            {
                selectedAmmoBoxPrefab = ammoBoxPrefabs[i];
                break;
            }
        }

        if (selectedAmmoBoxPrefab != null)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Vector3 spawnPosition = GetRandomSpawnPosition(randomSpawnPoint);

            GameObject spawnedAmmoBox = Instantiate(selectedAmmoBoxPrefab, spawnPosition, Quaternion.identity);
            if (spawnedAmmoBox.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.Spawn();
                Debug.Log($"Server: Spawned Ammo Box of type: {selectedAmmoBoxPrefab.name} at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"Ammo Box Prefab {selectedAmmoBoxPrefab.name} does not have a NetworkObject component!");
                Destroy(spawnedAmmoBox);
            }
        }
        else
        {
            Debug.LogError("Failed to select Ammo Box Prefab for spawning!");
        }
    }
}

// 🔁 Shuffle Extension
public static class ListExtensions
{
    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
