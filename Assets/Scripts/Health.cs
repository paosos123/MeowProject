using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // Import Random อีกครั้งเผื่อมีการใช้งานภายใน Health

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private NetworkVariable<int> currentHp = new NetworkVariable<int>();
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    [Header("Lives Settings")]
    public int maxLives = 3;
    private NetworkVariable<int> currentLives = new NetworkVariable<int>();

    [Header("Spawn Settings")]
    // public Vector3 startPosition; // ไม่ได้ใช้แล้ว

    [Header("HealthBarFill Settings")]
    public Image healthBarFill;

    [Header("Damage Settings")]
    [SerializeField] private int touchDamage = 10;

    public Action<Health> OnDie;

    private bool isDead;

    bool isHit = false;

    private GameOverUIController gameOverUIController;


    public override void OnNetworkSpawn()
    {
        // Subscribe to OnValueChanged event for currentHp ON ALL INSTANCES
        currentHp.OnValueChanged += OnHealthChanged;

        if (!IsServer) { return; }

        currentHp.Value = MaxHealth;
        currentLives.Value = maxLives;

        transform.position = SpawnPoint.GetRandomSpawnPos();

        gameOverUIController = GameObject.FindObjectOfType<GameOverUIController>();
        if (gameOverUIController == null)
        {
            Debug.LogError("ไม่พบ GameOverUIController ใน Scene!");
        }
    }
    void Update()
    {
        if (IsServer && Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20); // ทดสอบการลดเลือด
        }
    }

    // ฟังก์ชันสำหรับรับความเสียหาย (ทำงานบน Server เท่านั้น)
    public void TakeDamage(int damage)
    {
        if (currentHp.Value <= 0 || isHit)
        {
            return;
        }

        currentHp.Value -= damage;
        UpdateHealthBarFill(); // อัปเดตบน Server ด้วย
        Debug.Log($"Player (Server - TakeDamage): ได้รับความเสียหาย: {damage}, HP ปัจจุบัน: {currentHp.Value} (ClientId: {OwnerClientId})");
        StartCoroutine(GetHurt());

        if (currentHp.Value <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"Player (Server): ตาย! ชีวิตที่เหลือ: {currentLives.Value} (ClientId: {OwnerClientId})");
        currentLives.Value--;

        if (currentLives.Value > 0)
        {
            RespawnClientRpc(); // เรียก RPC ให้ Client ส่งคำขอ Respawn ไปยัง Server
        }
        else
        {
            GameOver();
        }
    }

    // ฟังก์ชันเรียก Server RPC เพื่อ Heal
    [ServerRpc]
    public void HealServerRpc(int amount)
    {
        if (isDead) return;

        currentHp.Value = Mathf.Min(currentHp.Value + amount, MaxHealth);
        Debug.Log($"Player (Server - HealServerRpc): ได้รับการรักษา: {amount}, HP ปัจจุบัน: {currentHp.Value} (ClientId: {OwnerClientId})");
    }

    // ฟังก์ชันที่ Client เรียกเพื่อขอ Heal
    public void Heal(int amount)
    {
        if (IsOwner && !isDead)
        {
            HealServerRpc(amount); // ส่งคำขอ Heal ไปยัง Server
        }
    }

    private void ModifyHealth(int value)
    {
        if (isDead) { return; }

        int newHealth = currentHp.Value + value;
        currentHp.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (currentHp.Value == 0)
        {
            OnDie?.Invoke(this);
            isDead = true;
        }
    }

    [ClientRpc] // Client RPC เพื่อส่งคำขอ Respawn ไปยัง Server
    void RespawnClientRpc()
    {
        if (IsOwner)
        {
            RespawnServerRpc(); // เรียก Server RPC เพื่อทำการ Respawn จริง
            transform.position = SpawnPoint.GetRandomSpawnPos(); // ตั้งตำแหน่งบน Client ทันทีเพื่อความราบรื่น
            UpdateHealthBarFill(); // อัปเดต UI บน Client ทันที
            Debug.Log($"Player (Client - Respawn): ร้องขอการเกิดใหม่ไปยัง Server (ClientId: {OwnerClientId})");
        }
    }

    [ServerRpc] // Server RPC เพื่อจัดการการ Respawn และตั้งค่า HP
    void RespawnServerRpc()
    {
        currentHp.Value = MaxHealth;
        Debug.Log($"Player (Server - RespawnServerRpc): ตั้งค่า HP เป็น {currentHp.Value} (ClientId: {OwnerClientId})");
    }

    void GameOver()
    {
        Debug.Log($"Player (Server): Game Over! ไม่มีชีวิตเหลือแล้ว (ClientId: {OwnerClientId})");
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(destroy: true); // หรือ Destroy(gameObject);
            // เรียกให้ GameOverUIController แสดง UI
            if (gameOverUIController != null)
            {
                gameOverUIController.ShowGameOver();
            }
            else
            {
                Debug.LogError("GameOverUIController ยังเป็น null ใน GameOver()!");
            }
        }

    }

    void UpdateHealthBarFill()
    {
        if (healthBarFill != null)
        {
            float fillAmount = (float)currentHp.Value / (float)MaxHealth;
            Debug.Log($"Client: Updating Heart Fill to {fillAmount} (ClientId: {OwnerClientId})");
            healthBarFill.fillAmount = fillAmount;
        }
        else
        {
            Debug.LogError($"Client: healthBarFill is null! (ClientId: {OwnerClientId})");
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBarFill(); // เรียก Update UI เมื่อค่า HP เปลี่ยนแปลง
    }

    IEnumerator GetHurt()
    {
        Physics2D.IgnoreLayerCollision(7, 8);
        ShowHurtClientRpc(true); // สั่งให้ Client แสดง Animation Hurt
        isHit = true;
        yield return new WaitForSeconds(2);
        ShowHurtClientRpc(false); // สั่งให้ Client หยุดแสดง Animation Hurt
        Physics2D.IgnoreLayerCollision(7, 8, false);
        isHit = false;
        if (currentHp.Value <= 0)
        {
            Die();
        }
    }

    [ClientRpc]
    void ShowHurtClientRpc(bool isHurt)
    {
        if (IsOwner)
        {
            GetComponent<Animator>().SetLayerWeight(1, isHurt ? 1 : 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            switch (other.tag)
            {
                case "Enemy":
                    TakeDamage(touchDamage);
                    break;
                case "EnemyBullet":
                    TakeDamage(touchDamage);
                    break;
                default:
                    break;
            }
        }
    }


}