using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

[System.Serializable]
public class GunData
{
    public string gunName;
    public Sprite gunSprite;
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    public float bulletSpeed = 10f;
    public float shootingDistance = 1.5f;
    public bool isUnlocked = false;
    public int bulletsPerShot = 1;
    public float spreadAngle = 0f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public float nextFireTime;
    public AudioClip fireSound;
}

public class GunController : NetworkBehaviour
{
    [SerializeField] private Animator shootingAnim;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform bulletTranform;
    [SerializeField] private SpriteRenderer gunRenderer;
    [SerializeField] private List<GunData> gunsData = new List<GunData>();
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private AudioSource gunAudioSource;

    private NetworkVariable<int> currentGunIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private GunData currentGunData;
    private bool isShooting = false;

    public override void OnNetworkSpawn()
    {
        currentGunIndex.OnValueChanged += OnCurrentGunIndexChanged;

        if (IsServer && gunsData.Count > 0 && gunRenderer != null)
        {
            gunsData[0].isUnlocked = true;
            gunsData[0].currentAmmo = gunsData[0].maxAmmo;
            Debug.Log($"Server: Gun {gunsData[0].gunName} unlocked and ammo set to {gunsData[0].currentAmmo}/{gunsData[0].maxAmmo}");
            UpdateCurrentGunIndexClientRpc(0, gunsData[0].currentAmmo);
        }
        else if (!IsServer && gunsData.Count > 0 && gunRenderer != null && currentGunIndex.Value < gunsData.Count)
        {
            Debug.Log($"Client (ClientId: {OwnerClientId}): OnNetworkSpawn - currentGunIndex.Value = {currentGunIndex.Value}, IsOwner: {IsOwner}");
            currentGunData = gunsData[currentGunIndex.Value];
            gunRenderer.sprite = currentGunData.gunSprite;
            if (IsOwner)
                UpdateAmmoUI();
            Debug.Log($"Client (ClientId: {OwnerClientId}): OnNetworkSpawn - Switched to {currentGunData.gunName}. Ammo: {currentGunData.currentAmmo}/{currentGunData.maxAmmo}");
        }
        else
        {
            Debug.LogWarning("No guns configured or Gun Renderer/AudioSource not assigned!");
        }
        if (gunAudioSource == null)
        {
            Debug.LogWarning("Gun AudioSource not assigned!");
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;
        ShootingAndMovePoint();
        HandleGunSwitching();
    }

    private void OnCurrentGunIndexChanged(int previousValue, int newValue)
    {
        Debug.Log($"Client (ClientId: {OwnerClientId}): currentGunIndex changed from {previousValue} to {newValue}, IsOwner: {IsOwner}");
        if (newValue >= 0 && newValue < gunsData.Count && gunRenderer != null)
        {
            if (currentGunData == null || currentGunData.gunName != gunsData[newValue].gunName)
            {
                currentGunData = gunsData[newValue];
            }
            gunRenderer.sprite = gunsData[newValue].gunSprite;
            if (IsOwner)
                UpdateAmmoUI();
        }
        else
        {
            Debug.LogError($"Client (ClientId: {OwnerClientId}): Invalid gun index or Gun Renderer not assigned!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchGunServerRpc(int index)
    {
        SwitchGun(index);
    }

    private void SwitchGun(int index)
    {
        if (!IsServer) return;

        if (index >= 0 && index < gunsData.Count && gunsData[index].isUnlocked)
        {
            currentGunIndex.Value = index;
            currentGunData = gunsData[index];
            currentGunData.nextFireTime = Time.time;
            Debug.Log($"Server: Switched to {currentGunData.gunName}. Ammo: {gunsData[index].currentAmmo}/{gunsData[index].maxAmmo}");
            UpdateCurrentGunIndexClientRpc(index, gunsData[index].currentAmmo);
        }
        else if (index >= 0 && index < gunsData.Count && !gunsData[index].isUnlocked)
        {
            Debug.Log($"You have not picked up the {gunsData[index].gunName} yet!");
        }
        else
        {
            Debug.LogError("Invalid gun index or Gun Renderer not assigned!");
        }
    }

    private void HandleGunSwitching()
    {
        if (!IsOwner) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput > 0f)
        {
            SwitchToNextUnlockedGunServerRpc();
        }
        else if (scrollInput < 0f)
        {
            SwitchToPreviousUnlockedGunServerRpc();
        }
    }

    [ServerRpc]
    private void SwitchToNextUnlockedGunServerRpc()
    {
        int nextIndex = currentGunIndex.Value;
        for (int i = 0; i < gunsData.Count; i++)
        {
            nextIndex = (nextIndex + 1) % gunsData.Count;
            if (gunsData[nextIndex].isUnlocked)
            {
                SwitchGunServerRpc(nextIndex);
                return;
            }
        }
    }

    [ServerRpc]
    private void SwitchToPreviousUnlockedGunServerRpc()
    {
        int previousIndex = currentGunIndex.Value;
        for (int i = 0; i < gunsData.Count; i++)
        {
            previousIndex = (previousIndex - 1 + gunsData.Count) % gunsData.Count;
            if (gunsData[previousIndex].isUnlocked)
            {
                SwitchGunServerRpc(previousIndex);
                return;
            }
        }
    }

    public void UnlockGun(string gunName)
    {
        if (!IsServer) return;

        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == gunName)
            {
                gunsData[i].isUnlocked = true;
                gunsData[i].currentAmmo = gunsData[i].maxAmmo;
                Debug.Log($"Server: Picked up {gunName}! Ammo full: {gunsData[i].currentAmmo}/{gunsData[i].maxAmmo}");
                SwitchGun(i);
                return;
            }
        }
        Debug.LogWarning($"Gun with name {gunName} not found!");
    }

    private void Shoot(float angle)
    {
        if (!IsOwner) return;

        // ตรวจสอบว่า Unlimited Ammo เปิดใช้งานอยู่หรือไม่
        if (!isUnlimitedAmmoActive)
        {
            if (currentGunData.currentAmmo <= 0)
            {
                Debug.Log($"Local Client (ClientId: {OwnerClientId}): Out of ammo for {currentGunData.gunName}!");
                return;
            }
        }

        // เล่นเสียงปืนบน Server และให้ทุกคนได้ยิน
        PlayGunSoundServerRpc(currentGunIndex.Value, OwnerClientId);

        // เรียกการยิง
        if (shootingAnim != null)
        {
            shootingAnim.SetTrigger("Shoot");
        }

        // ส่งข้อมูลไปที่ Server
        ShootServerRpc(shootingPoint.position, Quaternion.Euler(0, 0, angle), OwnerClientId);

        // ตั้งเวลาในการยิง
        currentGunData.nextFireTime = Time.time + currentGunData.fireRate;

        // อัพเดต UI
        UpdateAmmoUI();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayGunSoundServerRpc(int gunIndex, ulong shooterClientId)
    {
        // Play the sound on the server
        if (gunIndex >= 0 && gunIndex < gunsData.Count)
        {
            if (gunsData[gunIndex].fireSound != null)
            {
                //gunAudioSource.PlayOneShot(gunsData[gunIndex].fireSound); // Remove this line
                PlayGunSoundClientRpc(gunIndex, shooterClientId); //call the client RPC to play sound
            }
            else
            {
                Debug.LogWarning($"No fire sound assigned for {gunsData[gunIndex].gunName}!");
            }
        }
        else
        {
            Debug.LogError($"Invalid gun index {gunIndex} for playing sound! gunIndex: {gunIndex}, gunsData.Count: {gunsData.Count}");
        }
    }

    [ClientRpc]
    private void PlayGunSoundClientRpc(int gunIndex, ulong targetClientId)
    {
        // Play the sound on all clients
        if (gunAudioSource != null)
        {
            if (gunIndex >= 0 && gunIndex < gunsData.Count)
            {
                AudioClip soundToPlay = gunsData[gunIndex].fireSound;
                if (soundToPlay != null)
                {
                    gunAudioSource.PlayOneShot(soundToPlay);
                }
                else
                {
                    Debug.LogWarning($"No fire sound assigned for {gunsData[gunIndex].gunName}!");
                }
            }
            else
            {
                Debug.LogError($"Invalid gun index {gunIndex} for playing sound! gunIndex: {gunIndex}, gunsData.Count: {gunsData.Count}");
            }
        }
        else
        {
            Debug.LogError($"gunAudioSource is null! ClientId: {targetClientId}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(Vector3 position, Quaternion rotation, ulong shooterClientId)
    {
        // ตรวจสอบสถานะ Unlimited Ammo ของ Client ที่ยิง
        bool isShooterUnlimitedAmmoActive = false;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client))
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<GunController>(out var shooterGunController))
            {
                isShooterUnlimitedAmmoActive = shooterGunController.isUnlimitedAmmoActive;
            }
        }

        if (currentGunData == null || (!isShooterUnlimitedAmmoActive && currentGunData.currentAmmo < currentGunData.bulletsPerShot))
        {
            return;
        }

        // ลดกระสุนบน Server ถ้า Unlimited Ammo ไม่ได้เปิดใช้งาน
        if (!isShooterUnlimitedAmmoActive)
        {
            for (int i = 0; i < gunsData.Count; i++)
            {
                if (gunsData[i].gunName == currentGunData.gunName)
                {
                    gunsData[i].currentAmmo -= currentGunData.bulletsPerShot;
                    Debug.Log($"Server: Ammo reduced for {gunsData[i].gunName} by ClientId: {shooterClientId}. Current ammo: {gunsData[i].currentAmmo}");
                    UpdateAmmoClientRpc(gunsData[i].currentAmmo, shooterClientId);
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"Server: ClientId: {shooterClientId} shooting {currentGunData.gunName} with unlimited ammo.");
        }

        // Spawn กระสุนบน Server
        for (int i = 0; i < currentGunData.bulletsPerShot; i++)
        {
            float bulletAngle = rotation.eulerAngles.z + Random.Range(-currentGunData.spreadAngle, currentGunData.spreadAngle);
            Quaternion bulletRotation = Quaternion.Euler(0, 0, bulletAngle);

            GameObject bulletClone = Instantiate(currentGunData.bulletPrefab, position, bulletRotation);
            if (bulletClone.TryGetComponent<Rigidbody2D>(out Rigidbody2D bulletRb))
            {
                Vector2 fireDirection = bulletRotation * Vector2.right;
                bulletRb.linearVelocity = fireDirection * currentGunData.bulletSpeed;
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a Rigidbody2D component!");
                Destroy(bulletClone);
                continue;
            }

            if (bulletClone.TryGetComponent<NetworkObject>(out NetworkObject bulletNetworkObject))
            {
                bulletNetworkObject.Spawn(true);

                if (bulletClone.TryGetComponent<Bullet>(out Bullet bulletScript))
                {
                    bulletScript.SetShooter(shooterClientId);
                }
                else
                {
                    Debug.LogError("Bullet prefab does not have a Bullet script!");
                    bulletNetworkObject.Despawn(destroy: true);
                }
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a NetworkObject component!");
                Destroy(bulletClone);
            }
        }
    }

    private bool isUnlimitedAmmoActive = false;
    private float unlimitedAmmoDuration = 10f;
    private float unlimitedAmmoEndTime = 0f;

    [ClientRpc]
    private void UpdateAmmoClientRpc(int currentAmmo, ulong targetClientId)
    {
        if (IsOwner && OwnerClientId == targetClientId)
        {
            Debug.Log($"Server -> ClientRpc (Client {OwnerClientId}): UpdateAmmoClientRpc with ammo: {currentAmmo}");

            if (isUnlimitedAmmoActive)
            {
                currentGunData.currentAmmo = currentGunData.maxAmmo;
            }
            else
            {
                currentGunData.currentAmmo = currentAmmo;
            }

            UpdateAmmoUI();
            Debug.Log($"Client (ClientId: {OwnerClientId}): Updated local ammo to {currentGunData.currentAmmo}");
        }
    }

    public void ActivateUnlimitedAmmo()
    {
        if (isUnlimitedAmmoActive) return;

        isUnlimitedAmmoActive = true;
        unlimitedAmmoEndTime = Time.time + unlimitedAmmoDuration;
        Debug.Log("Unlimited ammo activated for 10 seconds.");

        currentGunData.currentAmmo = currentGunData.maxAmmo;

        Invoke(nameof(DeactivateUnlimitedAmmo), unlimitedAmmoDuration);

        UpdateUnlimitedAmmoClientRpc(true, OwnerClientId);
    }

    [ClientRpc]
    private void UpdateUnlimitedAmmoClientRpc(bool isActive, ulong targetClientId = 0)
    {
        if (!IsOwner || OwnerClientId != targetClientId) return;

        isUnlimitedAmmoActive = isActive;

        if (isActive)
        {
            currentGunData.currentAmmo = currentGunData.maxAmmo;
            Debug.Log("Unlimited Ammo is now active on the client side.");
        }
        else
        {
            Debug.Log("Unlimited Ammo deactivated on the client side.");
        }

        UpdateAmmoUI();
    }

    private void DeactivateUnlimitedAmmo()
    {
        if (Time.time >= unlimitedAmmoEndTime)
        {
            isUnlimitedAmmoActive = false;
            Debug.Log("Unlimited ammo deactivated.");
            UpdateUnlimitedAmmoClientRpc(false, OwnerClientId);
        }
    }

    public bool CanShoot()
    {
        if (isUnlimitedAmmoActive)
        {
            return true;
        }
        return currentGunData != null && currentGunData.currentAmmo > 0 && Time.time >= currentGunData.nextFireTime;
    }

    private void ShootingAndMovePoint()
    {
        if (!IsOwner) return;

        if (currentGunData == null || shootingPoint == null || bulletTranform == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        shootingPoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        shootingPoint.position = transform.position + Quaternion.Euler(0, 0, angle) * new Vector3(currentGunData.shootingDistance, 0, 0);

        bulletTranform.rotation = Quaternion.Euler(0, 0, angle);

        if (Input.GetMouseButton(0) && Time.time >= currentGunData.nextFireTime)
        {
            Shoot(angle);
            currentGunData.nextFireTime = Time.time + currentGunData.fireRate;
        }
    }

    public void AddAmmo(string gunName, int amount, ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == gunName)
            {
                gunsData[i].currentAmmo = Mathf.Min(gunsData[i].currentAmmo + amount, gunsData[i].maxAmmo);
                Debug.Log($"Server: Added {amount} ammo to {gunsData[i].gunName} for ClientId: {clientId}. Current ammo: {gunsData[i].currentAmmo}/{gunsData[i].maxAmmo}");
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<GunController>(out var gunController))
                    {
                        gunController.UpdateAmmoClientRpc(gunsData[i].currentAmmo, clientId);
                    }
                }
                return;
            }
        }
        Debug.LogWarning($"Gun with name {gunName} not found for ammo refill!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddAmmoServerRpc(string gunName, int amount, ulong clientId)
    {
        AddAmmo(gunName, amount, clientId);
    }

    public void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (isUnlimitedAmmoActive)
        {
            ammoText.text = $"{currentGunData.maxAmmo} / {currentGunData.maxAmmo}";
        }
        else
        {
            ammoText.text = $"{currentGunData.currentAmmo} / {currentGunData.maxAmmo}";
        }
    }

    [ClientRpc]
    private void UpdateCurrentGunIndexClientRpc(int newIndex, int initialAmmo)
    {
        Debug.Log($"Server -> ClientRpc (Client {OwnerClientId}): UpdateCurrentGunIndex to {newIndex} with initialAmmo {initialAmmo}");
        if (newIndex >= 0 && newIndex < gunsData.Count)
        {
            if (currentGunData == null || currentGunData.gunName != gunsData[newIndex].gunName)
            {
                currentGunData = gunsData[newIndex];
            }
            currentGunData.currentAmmo = initialAmmo;
            if (IsOwner)
                UpdateAmmoUI();
        }
    }

    public bool HasGun(string gunName)
    {
        foreach (var gun in gunsData)
        {
            if (gun.gunName == gunName && gun.isUnlocked)
            {
                return true;
            }
        }
        return false;
    }
}

