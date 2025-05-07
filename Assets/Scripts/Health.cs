using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // Import Random อีกครั้งเผื่อมีการใช้งานภายใน Health

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField]
    private NetworkVariable<int> currentHp = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
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
    [SerializeField] private int healthToRestore = 20; // เพิ่มตัวแปรนี้

    [Header("Audio Settings")]
    public AudioSource hurtAudioSource; // Drag AudioSource component มาใส่ใน Inspector
    public AudioClip ownerHurtSoundEffect; // เสียงสำหรับ Player ที่เป็นเจ้าของ
    public AudioClip otherHurtSoundEffect; // เสียงสำหรับ Player คนอื่น

    public Action<Health> OnDie;

    private bool isDead;
    bool isHit = false;

    public override void OnNetworkSpawn()
    {
        // Subscribe to OnValueChanged event for currentHp ON ALL INSTANCES
        currentHp.OnValueChanged += OnHealthChanged;

        if (!IsServer) { return; }

        currentHp.Value = MaxHealth;
        currentLives.Value = maxLives;

        transform.position = SpawnPoint.GetRandomSpawnPos();

        // ตรวจสอบ AudioSource
        if (hurtAudioSource == null)
        {
            Debug.LogWarning("ไม่ได้กำหนด AudioSource สำหรับเสียงเจ็บ!");
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
        Debug.Log($"[SERVER] Heal: +{amount}, HP now {currentHp.Value} (ClientId: {OwnerClientId})");
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
            GetComponent<NetworkObject>().Despawn(destroy: true);

            // เรียก RPC บน Singleton Instance ของ GameOverUIController และส่ง ClientId ของผู้เล่นที่ตาย
            if (GameOverUIController.Instance != null)
            {
                GameOverUIController.Instance.ShowGameOverClientRpc(OwnerClientId);
            }
            else
            {
                Debug.LogError("ไม่พบ GameOverUIController Instance บน Server!");
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
        Debug.Log($"[CLIENT] OnHealthChanged: {previousValue} -> {newValue} (ClientId: {OwnerClientId})");
        UpdateHealthBarFill();
    }

    IEnumerator GetHurt()
    {
        Physics2D.IgnoreLayerCollision(7, 8);
        ShowHurtClientRpc(true); // สั่งให้ Client แสดง Animation Hurt

        // เล่นเสียง Hurt เฉพาะบน Client ที่เป็นเจ้าของ Player
        PlayHurtSoundClientRpc();

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
        GetComponent<Animator>().SetLayerWeight(1, isHurt ? 1 : 0);
    }

    [ClientRpc]
    void PlayHurtSoundClientRpc()
    {
        if (hurtAudioSource != null)
        {
            if (IsOwner)
            {
                hurtAudioSource.PlayOneShot(ownerHurtSoundEffect);
                Debug.Log($"Client (Owner): เล่นเสียงเจ็บ (Owner) (ClientId: {OwnerClientId})");
            }
            else
            {
                hurtAudioSource.PlayOneShot(otherHurtSoundEffect);
                Debug.Log($"Client (Other): เล่นเสียงเจ็บ (Other) (ClientId: {OwnerClientId})");
            }
        }
    }

    [ServerRpc]
    private void RequestDespawnServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.Despawn();
        }
    }
    [ServerRpc]
    private void RequestDamageServerRpc(int amount)
    {
        TakeDamage(amount);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return; // ให้แค่ Owner ขอ heal

        if (other.CompareTag("HealthItem"))
        {
            HealServerRpc(healthToRestore); // ขอให้ Server heal
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                RequestDespawnServerRpc(netObj.NetworkObjectId);
            }
        }
        else if (other.CompareTag("Enemy") || other.CompareTag("EnemyBullet"))
        {
            Debug.Log("Client: Hit by enemy or bullet.");
            RequestDamageServerRpc(touchDamage);
        }
    }
}