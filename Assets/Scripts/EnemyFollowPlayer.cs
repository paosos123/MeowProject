using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // เพิ่ม namespace Linq

public class EnemyFollowClosestPlayerByTag2D : MonoBehaviour
{
    public string playerTag = "Player";
    public float moveSpeed = 5f;
    public int maxHp = 100;
    private int currentHp;
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // ตัวแปร SpriteRenderer

    void Start()
    {
        currentHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        FindClosestPlayer(); // ค้นหาผู้เล่นที่ใกล้ที่สุดเมื่อเริ่มเกม
    }

    void Update()
    {
        FindClosestPlayer(); // ค้นหาผู้เล่นที่ใกล้ที่สุดในทุกเฟรม

        if (targetPlayer != null)
        {
            MoveTowardsTarget();
            UpdateSpriteDirection(); // อัปเดตทิศทาง Sprite
        }
        else
        {
            StopMoving();
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void FindClosestPlayer()
    {
        // ค้นหา GameObject ทั้งหมดที่มี Tag ตามที่ระบุ
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(playerTag);

        if (playerObjects.Length == 0)
        {
            targetPlayer = null;
            return;
        }

        // แปลงเป็น List ของ Transform และเรียงตามระยะทาง
        List<Transform> playerTransforms = playerObjects
            .Select(go => go.transform)
            .OrderBy(t => Vector2.Distance(transform.position, t.position))
            .ToList();

        // กำหนดเป้าหมายเป็นผู้เล่นที่ใกล้ที่สุด (ถ้ามี)
        targetPlayer = playerTransforms.FirstOrDefault();
    }

    void MoveTowardsTarget()
    {
        if (targetPlayer != null)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed; // ใช้ rb.velocity แทน rb.linearVelocity ใน 2D
        }
        else
        {
            StopMoving();
        }
    }

    void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // ใช้ rb.velocity แทน rb.linearVelocity ใน 2D
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHp -= damageAmount;
        Debug.Log(gameObject.name + " received " + damageAmount + " damage. Current HP: " + currentHp);
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Bullet")) // ใช้ CompareTag เพื่อประสิทธิภาพที่ดีกว่า
        {
            // ทำลายกระสุนเมื่อชน
            Destroy(other.gameObject);
            // ทำดาเมจให้ศัตรู (สามารถปรับค่าดาเมจได้ตามต้องการ)
            TakeDamage(10);
        }
    }

    // ฟังก์ชันสำหรับอัปเดตทิศทาง Sprite ให้หันตามผู้เล่น
    void UpdateSpriteDirection()
    {
        if (targetPlayer != null && spriteRenderer != null)
        {
            if (targetPlayer.position.x > transform.position.x)
            {
                spriteRenderer.flipX = false; // ไม่พลิก (หันไปทางขวา)
            }
            else if (targetPlayer.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true; // พลิก (หันไปทางซ้าย)
            }
        }
    }
}