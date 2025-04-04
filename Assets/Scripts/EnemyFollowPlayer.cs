using System;
using UnityEngine;
using System.Collections.Generic;

public class EnemyFollowClosestPlayerByTag2D : MonoBehaviour
{
    public string playerTag = "Player";
    public float moveSpeed = 5f;
    public int maxHp = 100;
    private int currentHp;
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private List<Transform> players = new List<Transform>();
    private SpriteRenderer spriteRenderer; // เพิ่มตัวแปร SpriteRenderer

    void Start()
    {
        currentHp = maxHp;

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject playerObject in playerObjects)
        {
            players.Add(playerObject.transform);
        }

        if (players.Count == 0)
        {
            Debug.LogError("ไม่พบ GameObject ที่มี Tag '" + playerTag + "' โปรดตรวจสอบ Tag ของ Player ใน Inspector!");
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("Enemy ไม่มี Rigidbody2D อาจไม่สามารถเคลื่อนที่ได้");
        }

        // รับ Component SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Enemy ไม่มี SpriteRenderer!  การ Flip อาจไม่ทำงาน.");
        }
    }

    void Update()
    {
        FindClosestPlayer();

        if (targetPlayer != null)
        {
            MoveTowardsTarget();
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
        if (players == null || players.Count == 0)
        {
            targetPlayer = null;
            return;
        }

        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        Vector2 enemyPosition = transform.position;

        foreach (Transform player in players)
        {
            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(enemyPosition, player.position);
                if (distanceToPlayer < closestDistance)
                {
                    closestDistance = distanceToPlayer;
                    closestPlayer = player;
                }
            }
        }

        targetPlayer = closestPlayer;
    }

    void MoveTowardsTarget()
    {
        if (rb != null)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPlayer.position, moveSpeed * Time.deltaTime);
        }
    }

    void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
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
        if (other.gameObject.tag == "Bullet")
        {
            Destroy(other.gameObject);
        }
    }
}
