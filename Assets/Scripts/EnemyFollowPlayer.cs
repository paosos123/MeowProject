using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class EnemyFollowClosestPlayerByTag2D : NetworkBehaviour
{
    public string playerTag = "Player";
    public float moveSpeed = 5f;
    public int maxHp = 100;
    public float searchInterval = 0.5f; // กำหนดช่วงเวลาในการค้นหาผู้เล่นใหม่

    private NetworkVariable<int> currentHp = new NetworkVariable<int>();
    private NetworkVariable<NetworkObjectReference> targetPlayerRef = new NetworkVariable<NetworkObjectReference>();
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float lastSearchTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHp.Value = maxHp;
            lastSearchTime = Time.time; // เริ่มนับเวลา
            FindClosestPlayerServerRpc(); // ค้นหาผู้เล่นครั้งแรกเมื่อ Spawn
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>(); // อย่าลืม Get Component Rigidbody2D ด้วยครับ
    }

    void Update()
    {
        // ทำงานบน Client เพื่ออัปเดต targetPlayer จาก NetworkVariable
        if (IsClient && targetPlayerRef.Value.TryGet(out NetworkObject targetNO))
        {
            targetPlayer = targetNO.transform;
        }

        // อัปเดตทิศทาง Sprite บน Client ที่เป็นเจ้าของ
        if (IsClient && IsOwner && targetPlayer != null)
        {
            UpdateSpriteDirection();
        }

        // ค้นหาผู้เล่นที่ใกล้ที่สุดเป็นระยะบน Server
        if (IsServer && Time.time >= lastSearchTime + searchInterval)
        {
            FindClosestPlayerServerRpc();
            lastSearchTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        // ให้การเคลื่อนที่ทำงานบน Server เท่านั้น
        if (!IsServer || targetPlayer == null || rb == null) return; // ตรวจสอบ rb ด้วย

        Vector2 direction = (targetPlayer.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed; // ใช้ rb.velocity แทน rb.linearVelocity ใน 2D
    }

    [ServerRpc]
    private void FindClosestPlayerServerRpc()
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(playerTag);

        if (playerObjects.Length == 0)
        {
            targetPlayerRef.Value = new NetworkObjectReference();
            targetPlayer = null;
            return;
        }

        GameObject closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject player in playerObjects)
        {
            float distanceToPlayer = Vector3.Distance(currentPosition, player.transform.position);
            if (distanceToPlayer < closestDistance)
            {
                closestPlayer = player;
                closestDistance = distanceToPlayer;
            }
        }

        if (closestPlayer != null)
        {
            targetPlayerRef.Value = closestPlayer.GetComponent<NetworkObject>();
            targetPlayer = closestPlayer.transform;
        }
        else
        {
            targetPlayerRef.Value = new NetworkObjectReference();
            targetPlayer = null;
        }
    }

    void MoveTowardsTarget()
    {
        // การเคลื่อนที่ถูกจัดการใน FixedUpdate บน Server แล้ว
    }

    void StopMoving()
    {
        if (IsServer && rb != null)
        {
            rb.linearVelocity = Vector2.zero; // ใช้ rb.velocity แทน rb.linearVelocity ใน 2D
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damageAmount)
    {
        if (!IsServer) return;

        currentHp.Value -= damageAmount;
        Debug.Log($"Server: {gameObject.name} received {damageAmount} damage. Current HP: {currentHp.Value}");

        if (currentHp.Value <= 0)
        {
            DieClientRpc();
        }
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Debug.Log($"Client: {gameObject.name} has died.");
        Destroy(gameObject);
    }

 

    void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || targetPlayer == null) return;

        if (targetPlayer.position.x > transform.position.x)
        {
            spriteRenderer.flipX = false;
        }
        else if (targetPlayer.position.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsServer) return; // ทำงานเฉพาะบน Server

        if (other.CompareTag("Enemy") && other != GetComponent<Collider2D>())
        {
            // พบ Enemy ตัวอื่นที่กำลังซ้อนทับ
            Vector2 direction = (transform.position - other.transform.position).normalized;
            float separationForce = 0.1f; // ปรับค่านี้เพื่อกำหนดแรงผลัก

            // ปรับตำแหน่ง Enemy ตัวนี้
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position += direction * separationForce * Time.fixedDeltaTime;
            }
            else
            {
                transform.position += (Vector3)direction * separationForce * Time.fixedDeltaTime;
            }

            // ปรับตำแหน่ง Enemy ตัวอื่นด้วย (ถ้าต้องการให้ผลักทั้งคู่)
            Rigidbody2D otherRb = other.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                otherRb.position -= direction * separationForce * Time.fixedDeltaTime;
            }
            else
            {
                other.transform.position -= (Vector3)direction * separationForce * Time.fixedDeltaTime;
            }
        }
    }
}