using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class EnemyFollowAndStopToShoot2D : NetworkBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    private NetworkVariable<NetworkObjectReference> targetPlayerRef = new NetworkVariable<NetworkObjectReference>();
    private Transform targetPlayer;
    private List<Transform> players = new List<Transform>();

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    [Header("Shooting")]
    public float shootingRange = 3f;
    public GameObject bulletPrefab;
    public Transform bulletTranform;
    public float bulletSpeed = 10f;
    public float fireRate = 0.5f;
    private float nextFireTime;
    private bool isWithinShootingRange = false;

    [Header("Health")]
    public int maxHp = 100;
    private NetworkVariable<int> currentHp = new NetworkVariable<int>();

    [Header("Visuals")]
    private bool facingRight = true; // ตัวแปรเพื่อเก็บว่า Enemy หันไปทางขวาหรือไม่
    private SpriteRenderer spriteRenderer;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHp.Value = maxHp;
        }

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        nextFireTime = Time.time;
    }

    void Update()
    {
        // ทำงานบน Client เพื่ออัปเดต targetPlayer จาก NetworkVariable
        if (IsClient && targetPlayerRef.Value.TryGet(out NetworkObject targetNO))
        {
            targetPlayer = targetNO.transform;
        }

        if (IsServer)
        {
            FindClosestPlayerServerRpc(); // ค้นหาผู้เล่นในทุกเฟรม

            if (targetPlayer != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, targetPlayer.position);

                if (distanceToTarget <= shootingRange)
                {
                    isWithinShootingRange = true;
                    StopMovingServerRpc();
                    ShootServerRpc();
                    Debug.Log($"Server: {gameObject.name} - อยู่ในระยะยิง, กำลังยิง");
                }
                else
                {
                    isWithinShootingRange = false;
                    MoveTowardsTargetServerRpc();
                    Debug.Log($"Server: {gameObject.name} - นอกระยะยิง, กำลังเคลื่อนที่ไปยัง {targetPlayer.name}");
                }

                UpdateFacingDirectionServerRpc();
            }
            else
            {
                StopMovingServerRpc();
                Debug.Log($"Server: {gameObject.name} - ไม่พบเป้าหมาย, หยุดเคลื่อนที่");
            }

            if (currentHp.Value <= 0)
            {
                DieClientRpc();
            }
        }

        // อัปเดตทิศทาง Sprite บน Client ที่เป็นเจ้าของ
        if (IsClient && IsOwner)
        {
            UpdateSpriteDirection();
        }
    }

    void FixedUpdate()
    {
        // การเคลื่อนที่ถูกจัดการโดย ServerRpc แล้ว
    }

    [ServerRpc]
    private void FindClosestPlayerServerRpc()
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(playerTag);
        Debug.Log($"Server: {gameObject.name} - พบผู้เล่น {playerObjects.Length} คน");

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
            Debug.Log($"Server: {gameObject.name} - พบเป้าหมาย: {targetPlayer.name}");
        }
        else
        {
            targetPlayerRef.Value = new NetworkObjectReference();
            targetPlayer = null;
            Debug.Log($"Server: {gameObject.name} - ไม่พบเป้าหมาย");
        }
    }

    [ServerRpc]
    private void MoveTowardsTargetServerRpc()
    {
        if (targetPlayer != null && rb != null)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            StopMovingServerRpc();
        }
    }

    [ServerRpc]
    private void StopMovingServerRpc()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
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

    [ServerRpc]
    private void ShootServerRpc()
    {
        if (Time.time >= nextFireTime && isWithinShootingRange && targetPlayer != null && bulletPrefab != null && bulletTranform != null)
        {
            nextFireTime = Time.time + fireRate;

            Vector2 direction = (targetPlayer.position - bulletTranform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject bulletClone = Instantiate(bulletPrefab);
            bulletClone.transform.position = bulletTranform.position;
            bulletClone.transform.rotation = Quaternion.Euler(0, 0, angle);

            if (bulletClone.TryGetComponent<NetworkObject>(out NetworkObject bulletNetworkObject))
            {
                bulletNetworkObject.Spawn(true);
                Rigidbody2D bulletRb = bulletClone.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.linearVelocity = direction * bulletSpeed;
                }
                else
                {
                    Debug.LogError("Bullet prefab does not have a Rigidbody2D component!");
                    Destroy(bulletClone);
                }
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a NetworkObject component!");
                Destroy(bulletClone);
            }
        }
    }

    [ServerRpc]
    private void UpdateFacingDirectionServerRpc()
    {
        if (targetPlayer != null)
        {
            bool newFacingRight = targetPlayer.position.x > transform.position.x;
            if (newFacingRight != facingRight)
            {
                facingRight = newFacingRight;
                Flip180ClientRpc(facingRight);
            }
        }
    }

    [ClientRpc]
    private void Flip180ClientRpc(bool _facingRight)
    {
        facingRight = _facingRight;
        UpdateSpriteDirection();
    }

    void UpdateSpriteDirection()
    {
        if (spriteRenderer == null) return;

        Vector3 theScale = transform.localScale;
        theScale.x = Mathf.Abs(theScale.x) * (facingRight ? 1 : -1);
        transform.localScale = theScale;
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