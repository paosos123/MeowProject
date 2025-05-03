using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public enum BulletType
    {
        Pistol,
        AK,
        Shotgun
    }

    public BulletType bulletType = BulletType.Pistol;
    [SerializeField] private float lifeTime = 1.5f;
    private float timer;
    private Rigidbody2D rb;
    [SerializeField] public int damageAmount = 1; // ความเสียหายพื้นฐาน

    private ulong shooterId; // Store the NetworkObjectId of the player that shot this bullet

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        timer = 0f;
    }

    public void SetShooter(ulong id)
    {
        shooterId = id;
    }

    void Update()
    {
        if (IsOwner && rb != null)
        {
            if (rb.linearVelocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        if (IsServer)
        {
            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                Destroy(gameObject);
            }
            else if (other.gameObject.CompareTag("Player"))
            {
                if (other.TryGetComponent<NetworkObject>(out NetworkObject playerNetworkObject))
                {
                    // ดึง Component Health จาก Player และเรียก TakeDamage
                    if (playerNetworkObject.TryGetComponent<Health>(out Health playerHealth))
                    {
                        if (playerNetworkObject.OwnerClientId != shooterId) // Check if this is the shooter
                        {
                            playerHealth.TakeDamage(GetDamageAmount());
                            Destroy(gameObject); // ทำลายกระสุนหลังจากที่ทำดาเมจแล้ว
                        }
                        else
                        {
                            //Debug.Log("Bullet hit the player that shot it. Ignoring."); // Optionally log
                            //  Destroy(gameObject); //destroy it.
                        }
                    }
                    else
                    {
                        Debug.LogError("Player NetworkObject does not have a Health component!");
                        Destroy(gameObject);
                    }
                }
            }
            else if (other.gameObject.CompareTag("Enemy"))
            {
                if (other.TryGetComponent<NetworkObject>(out NetworkObject enemyNetworkObject))
                {
                    if (enemyNetworkObject.TryGetComponent<EnemyFollowAndStopToShoot2D>(out EnemyFollowAndStopToShoot2D enemyScriptWithShoot))
                    {
                        enemyScriptWithShoot.TakeDamageServerRpc(GetDamageAmount());
                        Destroy(gameObject);
                    }
                    else if (enemyNetworkObject.TryGetComponent<EnemyFollowClosestPlayerByTag2D>(out EnemyFollowClosestPlayerByTag2D enemyScriptFollow))
                    {
                        enemyScriptFollow.TakeDamageServerRpc(GetDamageAmount());
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.LogError("Enemy NetworkObject does not have a supported Enemy script!");
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning("ชนศัตรู แต่ไม่มี NetworkObject!");
                    Destroy(gameObject);
                }
            }
        }
    }

    // Getter สำหรับเข้าถึงค่าความเสียหาย
    public int GetDamageAmount()
    {
        return damageAmount;
    }
}

