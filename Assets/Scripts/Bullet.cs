using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private float timer;
    private Rigidbody2D rb;
    [SerializeField]private int damageAmount = 1;

   

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
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
            if (timer >= 1.5f)
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
                    if (!playerNetworkObject.IsOwner) // ถ้าชน Player อื่น
                    {
                        // แจ้ง Server ให้ลด HP ของ Player ที่ถูกชน
                        DealDamageServerRpc(playerNetworkObject, damageAmount);
                        Destroy(gameObject);
                    }
                    else // ชนตัวเอง (อาจเกิดขึ้นได้ยาก)
                    {
                        if (other.TryGetComponent<Health>(out Health playerHealth))
                        {
                            playerHealth.TakeDamage(damageAmount); // ลด HP บน Server โดยตรง
                            Destroy(gameObject);
                        }
                    }
                }
            }
            else if (other.gameObject.CompareTag("Enemy"))
            {
                if (other.TryGetComponent<Health>(out Health enemyHealth))
                {
                    DealDamageServerRpc(other.GetComponent<NetworkObject>(), damageAmount); // สมมติ Enemy ก็มี Health และ ServerRpc
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("ชนศัตรู แต่ไม่มี Script Health!");
                    Destroy(gameObject);
                }
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(NetworkObjectReference targetNetworkObject, int damage)
    {
        if (targetNetworkObject.TryGet(out NetworkObject targetNO))
        {
            if (targetNO.TryGetComponent<Health>(out Health targetHealth))
            {
                targetHealth.TakeDamage(damage); // เรียก TakeDamage บน Server โดยตรง
            }
            else
            {
                Debug.LogError("Target NetworkObject does not have Health component!");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve Target NetworkObjectReference!");
        }
    }
}