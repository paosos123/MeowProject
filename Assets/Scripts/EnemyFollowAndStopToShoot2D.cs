using UnityEngine;
using System.Collections.Generic;
using System.Linq; // เพิ่ม namespace Linq เพื่อใช้ OrderBy

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
    bool isWithinShootingRange = false;

    void Start()
    {
        currentHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
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
        // ค้นหา GameObject ทั้งหมดที่มี Tag ตามที่ระบุใน playerTag ทุกเฟรม
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(playerTag);

        // แปลงเป็น List ของ Transform และเรียงตามระยะทางจาก Enemy
        players = playerObjects
            .Select(go => go.transform)
            .OrderBy(t => Vector2.Distance(transform.position, t.position))
            .ToList();

        // กำหนด targetPlayer เป็น Player ที่ใกล้ที่สุด (ถ้ามี Player อยู่ใน Scene)
        targetPlayer = players.FirstOrDefault();
    }

    void MoveTowardsTarget()
    {
        if (targetPlayer != null)
        {
            if (rb != null)
            {
                Vector2 direction = (targetPlayer.position - transform.position).normalized;
                rb.linearVelocity = direction * moveSpeed; // ใช้ rb.velocity แทน rb.linearVelocity ใน 2D
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPlayer.position, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            StopMoving(); // ถ้าไม่มี targetPlayer ให้หยุดเคลื่อนที่
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

    void Shoot()
    {
        if (Time.time >= nextFireTime && isWithinShootingRange && targetPlayer != null) // ตรวจสอบ targetPlayer อีกครั้งก่อนยิง
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
                bulletRb.linearVelocity = direction * bulletSpeed; // ใช้ bulletRb.velocity แทน bulletRb.linearVelocity ใน 2D
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
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}