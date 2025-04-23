using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField]private NetworkVariable<int> currentHp = new NetworkVariable<int>();
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    [Header("Lives Settings")]
    public int maxLives = 3;
    private NetworkVariable<int> currentLives = new NetworkVariable<int>();

    [Header("Spawn Settings")]
    public Vector3 startPosition;

    [Header("HealthBarFill Settings")]
    public Image healthBarFill;

    [Header("Damage Settings")]
    [SerializeField] private int touchDamage = 20;

    public Action<Health> OnDie;

    private bool isDead;

    bool isHit = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        currentHp.Value = MaxHealth;
    }

    void Update()
    {
        if (IsServer && Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20);
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
        Debug.Log($"Player (Server - TakeDamage): ได้รับความเสียหาย: {damage}, HP ปัจจุบัน: {currentHp.Value} (ClientId: {OwnerClientId})");
        StartCoroutine(GetHurt());

        if (currentHp.Value <= 0)
        {
            Die();
        }
    }

    // ลบ TakeDamageServerRpc เก่า

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
        currentHp.Value = MaxHealth;
        transform.position = startPosition;
        Debug.Log($"Player (Server): เกิดใหม่ HP: {currentHp.Value}, ชีวิตที่เหลือ: {currentLives.Value} (ClientId: {OwnerClientId})");
    }

    void GameOver()
    {
        Debug.Log($"Player (Server): Game Over! ไม่มีชีวิตเหลือแล้ว (ClientId: {OwnerClientId})");
        // Logic Game Over
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