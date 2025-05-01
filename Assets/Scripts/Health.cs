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
        if (!IsServer) { return; }

        currentHp.Value = MaxHealth;
        currentLives.Value = maxLives;

        transform.position = SpawnPoint.GetRandomSpawnPos();

        gameOverUIController = GameObject.FindObjectOfType<GameOverUIController>();
        if (gameOverUIController == null)
        {
            Debug.LogError("ไม่พบ GameOverUIController ใน Scene!");
        }

        // เก็บอ้างอิง GameObject และ Component ควบคุม Player
      
    }

    void Update()
    {
        if (IsServer && Input.GetKeyDown(KeyCode.Space))
        {
            //TakeDamage(20);
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
        UpdateHealthBarFill();
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
            Respawn();
        }
        else
        {
            GameOver();
        }
    }

    // ฟังก์ชันสำหรับเพิ่มเลือด (ทำงานบน Client ที่เป็นเจ้าของ Player)
    [ClientRpc]
    public void HealClientRpc(int amount)
    {
        if (!IsOwner || isDead) return;

        currentHp.Value = Mathf.Min(currentHp.Value + amount, MaxHealth);
        UpdateHealthBarFill();
        Debug.Log($"Player (Client - Heal): ได้รับการรักษา: {amount}, HP ปัจจุบัน: {currentHp.Value} (ClientId: {OwnerClientId})");
    }

    // ฟังก์ชันเรียก RPC Heal บน Client จาก Server
    public void Heal(int amount)
    {
        HealClientRpc(amount);
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

    void Respawn()
    {
        if (!IsServer) return; // ให้ Server จัดการการเกิดใหม่

        currentHp.Value = MaxHealth;
        transform.position = SpawnPoint.GetRandomSpawnPos(); // ใช้ SpawnPoint ในการกำหนดตำแหน่งเกิดใหม่
        UpdateHealthBarFill();
        Debug.Log($"Player (Server): เกิดใหม่ HP: {currentHp.Value}, ชีวิตที่เหลือ: {currentLives.Value} ที่ตำแหน่ง: {transform.position} (ClientId: {OwnerClientId})");
    }

    void GameOver()
    {
        Debug.Log($"Player (Server): Game Over! ไม่มีชีวิตเหลือแล้ว (ClientId: {OwnerClientId})");
      
    }

    void UpdateHealthBarFill()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHp.Value / (float)MaxHealth;
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBarFill();
    }

    IEnumerator GetHurt()
    {
        Physics2D.IgnoreLayerCollision(7, 8);
        GetComponent<Animator>().SetLayerWeight(1, 1);
        isHit = true;
        yield return new WaitForSeconds(2);
        GetComponent<Animator>().SetLayerWeight(1, 0);
        Physics2D.IgnoreLayerCollision(7, 8, false);
        isHit = false;
        if (currentHp.Value <= 0)
        {
            Die();
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