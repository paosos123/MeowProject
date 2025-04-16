using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private float timer;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (IsOwner && rb != null) // หมุนเฉพาะบน Owner เพื่อความราบรื่น
        {
            if (rb.linearVelocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        if (IsServer) // ควบคุมการทำลายบน Server
        {
            timer += Time.deltaTime;
            if (timer >= 1.5f)
            {
                Destroy(gameObject); // Server ทำลาย
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer) // ตรวจสอบการชนและการทำลายบน Server
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                Destroy(gameObject); // Server ทำลาย
            }
            // เพิ่ม Logic การชนกับผู้เล่นหรือเป้าหมายอื่นๆ ที่นี่
        }
    }
}