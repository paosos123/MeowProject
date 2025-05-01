using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class AmmoBoxSpawner : NetworkBehaviour
{
    [Header("Ammo Box Prefabs")]
    public List<GameObject> ammoBoxPrefabs; // List ของ Ammo Box Prefab ทั้ง 3 ประเภท

    [Header("Spawn Settings")]
    public List<Transform> spawnPoints;
    public float spawnInterval = 10f; // ช่วงเวลาในการ Spawn (วินาที)

    private float nextSpawnTime;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnAmmoBox();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnAmmoBox()
    {
        if (ammoBoxPrefabs == null || ammoBoxPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("โปรดกำหนด Ammo Box Prefabs และ Spawn Points ให้ครบ!");
            return;
        }

        // กำหนดน้ำหนักความน่าจะเป็นในการเกิด (อันแรกเยอะสุด)
        float[] spawnProbabilities = new float[ammoBoxPrefabs.Count];
        float totalProbability = 0f;

        for (int i = 0; i < ammoBoxPrefabs.Count; i++)
        {
            spawnProbabilities[i] = ammoBoxPrefabs.Count - i; // ให้น้ำหนักลดลงตามลำดับ
            totalProbability += spawnProbabilities[i];
        }

        // สุ่มเลือกประเภท Ammo Box ตามน้ำหนัก
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
            // สุ่มเลือกจุด Spawn
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

            // ทำการ Spawn Ammo Box ผ่าน Network
            GameObject spawnedAmmoBox = Instantiate(selectedAmmoBoxPrefab, randomSpawnPoint.position, Quaternion.identity);
            if (spawnedAmmoBox.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.Spawn();
                Debug.Log($"Server: Spawned Ammo Box of type: {selectedAmmoBoxPrefab.name} at {randomSpawnPoint.position}");
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