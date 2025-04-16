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
    public int bulletsPerShot = 5;
    public float spreadAngle = 15f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public float nextFireTime;
}

public class GunController : NetworkBehaviour
{
    [SerializeField] private Animator shootingAnim;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform bulletTranform;
    [SerializeField] private SpriteRenderer gunRenderer;
    [SerializeField] private List<GunData> gunsData = new List<GunData>();
    [SerializeField] private TMP_Text ammoText;

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
            UpdateAmmoUI();
            Debug.Log($"Client (ClientId: {OwnerClientId}): OnNetworkSpawn - Switched to {currentGunData.gunName}. Ammo: {currentGunData.currentAmmo}/{currentGunData.maxAmmo}");
        }
        else
        {
            Debug.LogWarning("No guns configured or Gun Renderer not assigned!");
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
            currentGunData = gunsData[newValue];
            gunRenderer.sprite = currentGunData.gunSprite;
            UpdateAmmoUI();
            Debug.Log($"Client (ClientId: {OwnerClientId}): Switched to {currentGunData.gunName}. Ammo: {currentGunData.currentAmmo}/{currentGunData.maxAmmo}");
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
            currentGunData = gunsData[currentGunIndex.Value];
            currentGunData.nextFireTime = Time.time;
            Debug.Log($"Server: Switched to {currentGunData.gunName}. Ammo: {currentGunData.currentAmmo}/{currentGunData.maxAmmo}");
            UpdateCurrentGunIndexClientRpc(index, currentGunData.currentAmmo);
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

        if (currentGunData == null || currentGunData.currentAmmo <= 0 || Time.time < currentGunData.nextFireTime)
        {
            if (currentGunData != null && currentGunData.currentAmmo <= 0)
            {
                Debug.Log($"Local Client (ClientId: {OwnerClientId}): Out of ammo for {currentGunData.gunName}!");
            }
            return;
        }

        if (shootingAnim != null)
        {
            shootingAnim.SetTrigger("Shoot");
        }

        // เรียก ServerRpc เพื่อแจ้งให้ Server ทำการยิง
        ShootServerRpc(shootingPoint.position, Quaternion.Euler(0, 0, angle));

        // **ลบการลดกระสุนฝั่ง Client ออก**
        // currentGunData.currentAmmo -= currentGunData.bulletsPerShot;
        Debug.Log($"Local Client (ClientId: {OwnerClientId}): Requested to shoot {currentGunData.gunName}");
        currentGunData.nextFireTime = Time.time + currentGunData.fireRate;

        UpdateAmmoUI();
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Quaternion rotation)
    {
        if (currentGunData == null || currentGunData.currentAmmo < currentGunData.bulletsPerShot)
        {
            return;
        }

        // ลดกระสุนบน Server
        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == currentGunData.gunName)
            {
                gunsData[i].currentAmmo -= currentGunData.bulletsPerShot;
                Debug.Log($"Server: Ammo reduced for {gunsData[i].gunName}. Current ammo: {gunsData[i].currentAmmo}");
                UpdateAmmoClientRpc(gunsData[i].currentAmmo);
                break;
            }
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
            }

            if (bulletClone.TryGetComponent<NetworkObject>(out NetworkObject bulletNetworkObject))
            {
                bulletNetworkObject.Spawn(true);
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a NetworkObject component!");
            }
        }
    }

    [ClientRpc]
    private void UpdateAmmoClientRpc(int currentAmmo)
    {
        Debug.Log($"Client (ClientId: {OwnerClientId}): Received UpdateAmmoClientRpc with ammo: {currentAmmo}");
        if (!IsOwner) return;
        if (currentGunData != null)
        {
            currentGunData.currentAmmo = currentAmmo;
            UpdateAmmoUI();
            Debug.Log($"Client (ClientId: {OwnerClientId}): Updated local ammo to {currentGunData.currentAmmo}");
        }
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

    public void AddAmmo(string gunName, int amount)
    {
        if (!IsServer) return;

        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == gunName)
            {
                gunsData[i].currentAmmo = Mathf.Min(gunsData[i].currentAmmo + amount, gunsData[i].maxAmmo);
                Debug.Log($"Server: Added {amount} ammo to {gunsData[i].gunName}. Current ammo: {gunsData[i].currentAmmo}/{gunsData[i].maxAmmo}");
                if (currentGunData != null && currentGunData.gunName == gunName)
                {
                    UpdateAmmoClientRpc(gunsData[i].currentAmmo);
                }
                return;
            }
        }
        Debug.LogWarning($"Gun with name {gunName} not found for ammo refill!");
    }

    private void UpdateAmmoUI()
    {
        Debug.Log($"UpdateAmmoUI() called on ClientId: {OwnerClientId}, IsOwner: {IsOwner}");
        if (!IsOwner) return;

        Debug.Log($"Updating Ammo UI on ClientId: {OwnerClientId}");
        if (ammoText != null && currentGunData != null)
        {
            ammoText.text = $"{currentGunData.currentAmmo} / {currentGunData.maxAmmo}";
            Debug.Log($"UI Updated (Client {OwnerClientId}): Ammo = {currentGunData.currentAmmo} / {currentGunData.maxAmmo}, Text: {ammoText.text}");
        }
        else if (ammoText == null)
        {
            Debug.LogWarning($"Ammo Text (TMP_Text) not assigned in the Inspector on ClientId: {OwnerClientId}!");
        }
        else if (currentGunData == null)
        {
            Debug.LogWarning($"currentGunData is null on ClientId: {OwnerClientId}!");
        }
    }

    [ClientRpc]
    private void UpdateCurrentGunIndexClientRpc(int newIndex, int initialAmmo)
    {
        Debug.Log($"Server -> ClientRpc (Client {OwnerClientId}): UpdateCurrentGunIndex to {newIndex} with initialAmmo {initialAmmo}");
        currentGunIndex.Value = newIndex;
        if (newIndex >= 0 && newIndex < gunsData.Count)
        {
            if (currentGunData == null || currentGunData.gunName != gunsData[newIndex].gunName)
            {
                currentGunData = gunsData[newIndex];
            }
            currentGunData.currentAmmo = initialAmmo;
            UpdateAmmoUI();
        }
    }
}