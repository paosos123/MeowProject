using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class GunSpawner : NetworkBehaviour
{
    [Header("Gun Prefabs")]
    public GameObject gunPrefabType1;
    public GameObject gunPrefabType2;

    [Header("Spawn Settings")]
    public int spawnCountType1 = 4;
    public int spawnCountType2 = 4;
    public List<Transform> spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnNetworkGuns();
        }
    }

    void SpawnNetworkGuns()
    {
        if (gunPrefabType1 == null || gunPrefabType2 == null || spawnPoints.Count < spawnCountType1 + spawnCountType2)
        {
            Debug.LogError("โปรดกำหนด Gun Prefab ให้ครบและมีจุด Spawn เพียงพอ!");
            return;
        }

        // สุ่มตำแหน่ง Spawn
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);

        // Spawn ปืนประเภทที่ 1
        for (int i = 0; i < spawnCountType1; i++)
        {
            if (availableSpawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, availableSpawnPoints.Count);
                Transform spawnPosition = availableSpawnPoints[randomIndex];
                GameObject spawnedGun = Instantiate(gunPrefabType1, spawnPosition.position, spawnPosition.rotation);
                if (spawnedGun.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
                {
                    networkObject.Spawn();
                }
                else
                {
                    Debug.LogError("Gun Prefab Type 1 does not have a NetworkObject component!");
                    Destroy(spawnedGun);
                }
                availableSpawnPoints.RemoveAt(randomIndex);
            }
            else
            {
                Debug.LogWarning("มีจุด Spawn ไม่เพียงพอสำหรับปืนประเภทที่ 1!");
                break;
            }
        }

        // Spawn ปืนประเภทที่ 2
        for (int i = 0; i < spawnCountType2; i++)
        {
            if (availableSpawnPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, availableSpawnPoints.Count);
                Transform spawnPosition = availableSpawnPoints[randomIndex];
                GameObject spawnedGun = Instantiate(gunPrefabType2, spawnPosition.position, spawnPosition.rotation);
                if (spawnedGun.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
                {
                    networkObject.Spawn();
                }
                else
                {
                    Debug.LogError("Gun Prefab Type 2 does not have a NetworkObject component!");
                    Destroy(spawnedGun);
                }
                availableSpawnPoints.RemoveAt(randomIndex);
            }
            else
            {
                Debug.LogWarning("มีจุด Spawn ไม่เพียงพอสำหรับปืนประเภทที่ 2!");
                break;
            }
        }
    }
}