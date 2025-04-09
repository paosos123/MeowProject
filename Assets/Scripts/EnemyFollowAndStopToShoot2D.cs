using UnityEngine;
using System.Collections.Generic;

public class EnemyFollowAndStopToShoot2D : MonoBehaviour
{
    public string playerTag = "Player";
    public float moveSpeed = 5f;
    public float shootingRange = 3f;
    public int maxHp = 100;
    private int currentHp;
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private List<Transform> players = new List<Transform>();

    public GameObject bulletPrefab;
    public Transform bulletTranform;
    public float bulletSpeed = 10f;
    public float fireRate = 0.5f;
    private float nextFireTime;

    private bool facingRight = true; // ตัวแปรเพื่อเก็บว่า Enemy หันไปทางขวาหรือไม่
        bool isWithinShootingRange =false;
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

        if (bulletPrefab == null || bulletTranform == null)
        {
            Debug.LogError("โปรดกำหนด bulletPrefab และ bulletTranform ใน Inspector!");
            enabled = false;
            return;
        }
        nextFireTime = Time.time;
    }

    void Update()
    {
        FindClosestPlayer();

        if (targetPlayer != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, targetPlayer.position);

            if (distanceToTarget <= shootingRange)
            {
                isWithinShootingRange = true;
                StopMoving();
                Shoot();
            }
            else
            {
                isWithinShootingRange = false;
                MoveTowardsTarget();
            }

            Rotate180TowardsTarget(); // เรียกใช้ฟังก์ชันหมุนตัว 180 องศา
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

    void Shoot()
    {
        if (Time.time >= nextFireTime && isWithinShootingRange)
        {
            nextFireTime = Time.time + fireRate;

            Vector2 direction = (targetPlayer.position - bulletTranform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject bulletClone = Instantiate(bulletPrefab);
            bulletClone.transform.position = bulletTranform.position;
            bulletClone.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D bulletRb = bulletClone.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a Rigidbody2D component!");
            }
        }
    }

    // ฟังก์ชันสำหรับหมุนตัว Enemy 180 องศา
    void Rotate180TowardsTarget()
    {
        if (targetPlayer != null)
        {
            // ถ้า Player อยู่ทางขวาของ Enemy และ Enemy ไม่ได้หันไปทางขวา ให้หมุน
            if (targetPlayer.position.x > transform.position.x && !facingRight)
            {
                Flip180();
            }
            // ถ้า Player อยู่ทางซ้ายของ Enemy และ Enemy หันไปทางขวา ให้หมุน
            else if (targetPlayer.position.x < transform.position.x && facingRight)
            {
                Flip180();
            }
        }
    }

    void Flip180()
    {
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
    }
}
