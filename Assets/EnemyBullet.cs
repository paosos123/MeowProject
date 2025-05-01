using Unity.Netcode;
using UnityEngine;

public class EnemyBullet : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 3f; // กำหนดเวลาให้กระสุนมีชีวิตอยู่ 3 วินาที
    private float timer;

    public override void OnNetworkSpawn()
    {
        timer = 0f;
    }

    void Update()
    {
        if (IsServer)
        {
            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }

    // ไม่จำเป็นต้องมี OnTriggerEnter2D หรือ DealDamageServerRpc ใน Script นี้
    // เนื่องจาก Logic การชนและการลด Health ถูกจัดการใน Script Bullet หลัก
}