using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class HealthItemSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject healthItemPrefab;
    [SerializeField] private float spawnInterval = 5f; // ช่วงเวลาในการสุ่มเกิดไอเทม (วินาที)
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(); // List ของตำแหน่งที่จะ Spawn ไอเทม
    [SerializeField] private int maxActiveItems = 3; // จำนวนไอเทม Health ที่สามารถมีอยู่ใน Scene ได้พร้อมกัน

    private float timer;
    private int activeItemCount = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // ทำงานเฉพาะบน Server

        // เริ่มการ Spawn ครั้งแรกหลังจาก Network Spawn
        InvokeRepeating(methodName: nameof(SpawnHealthItemNetwork), time: 0f, repeatRate: spawnInterval);
    }

    void Update()
    {
        if (!IsServer) return; // ทำงานเฉพาะบน Server

        // ตรวจสอบและนับจำนวนไอเทม Health ที่ยังอยู่ใน Scene บน Server
        activeItemCount = GameObject.FindGameObjectsWithTag("HealthItem").Length;

        // Timer จะถูกจัดการโดย InvokeRepeating
    }

    private void SpawnHealthItemNetwork()
    {
        if (!IsServer || activeItemCount >= maxActiveItems || spawnPoints.Count == 0) return;

        // สุ่มเลือกตำแหน่งจาก List ของ Spawn Points
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Vector3 spawnPosition = spawnPoints[randomIndex].position;

        // ทำการ Spawn ไอเทมผ่าน Network
        GameObject healthItem = Instantiate(healthItemPrefab, spawnPosition, Quaternion.identity);
        if (healthItem.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            networkObject.Spawn();
            Debug.Log($"Server: Spawned Health Item at {spawnPosition}");
        }
        else
        {
            Debug.LogError("Health Item Prefab does not have a NetworkObject component!");
            Destroy(healthItem); // ทำลาย instance ที่สร้างถ้าไม่มี NetworkObject
        }
    }

    // ไม่จำเป็นต้องมี OnDrawGizmosSelected แล้ว เนื่องจากเราใช้ตำแหน่งที่กำหนดไว้
}