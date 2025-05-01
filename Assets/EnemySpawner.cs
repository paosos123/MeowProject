using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class EnemySpawner : NetworkBehaviour
{
    public List<GameObject> enemyPrefabs; // List ของ Enemy Prefab
    public List<Transform> spawnPoints;
    public float enemiesPerPlayer = 2f; // จำนวน Enemy ต่อผู้เล่นหนึ่งคน
    public float spawnInterval = 2f; // ช่วงเวลาในการ Spawn Enemy (วินาที)
    public float startDelay = 1f; // ระยะเวลาก่อนเริ่ม Spawn ครั้งแรก (วินาที)

    private float nextSpawnTime;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        nextSpawnTime = Time.time + startDelay;
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemiesBasedOnPlayerCount();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // ทำการ Clean up Enemy ที่ถูกทำลายออกจาก List
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    private void SpawnEnemiesBasedOnPlayerCount()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || spawnPoints.Count == 0)
        {
            Debug.LogError("Enemy Prefabs หรือ Spawn Points ยังไม่ได้ถูกกำหนด!");
            return;
        }

        int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int targetEnemyCount = Mathf.FloorToInt(playerCount * enemiesPerPlayer);
        int enemiesToSpawnThisInterval = targetEnemyCount - spawnedEnemies.Count;

        for (int i = 0; i < enemiesToSpawnThisInterval; i++)
        {
            // สุ่มเลือก Enemy Prefab สำหรับ Enemy ตัวนี้
            GameObject enemyToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

            // สุ่มเลือกจุดเกิดสำหรับ Enemy ตัวนี้
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

            // ทำการ Spawn Enemy ผ่าน Network
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, randomSpawnPoint.position, Quaternion.identity);
            if (spawnedEnemy.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.Spawn(true);
                spawnedEnemies.Add(spawnedEnemy);
                Debug.Log($"Server: Spawned {enemyToSpawn.name} at {randomSpawnPoint.position}. Total enemies: {spawnedEnemies.Count}, Target: {targetEnemyCount}");
            }
            else
            {
                Debug.LogError($"{enemyToSpawn.name} Prefab ไม่มี NetworkObject Component!");
                Destroy(spawnedEnemy);
            }
        }
    }
}