using Unity.Netcode;
using UnityEngine;

public class ItemPickup : NetworkBehaviour
{
    public enum ItemType
    {
        Gun,
        Health,
        UnlimitedAmmo 
    }

    public ItemType itemType;
    public string gunToUnlock;
    public int healthToRestore = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<NetworkObject>(out NetworkObject playerNetworkObject))
            {
                switch (itemType)
                {
                    case ItemType.Gun:
                        if (playerNetworkObject.TryGetComponent<GunController>(out var gunController))
                        {
                            if (!gunController.HasGun(gunToUnlock))
                            {
                                gunController.UnlockGun(gunToUnlock);
                                Destroy(gameObject); // ✅ ลบที่นี่บน Server
                            }
                            else
                            {
                                Debug.Log($"Server: Player already has gun {gunToUnlock}, item not destroyed.");
                            }
                        }
                        break;

                    case ItemType.Health:
                        if (playerNetworkObject.TryGetComponent<Health>(out var health))
                        {
                            health.Heal(healthToRestore);
                            Destroy(gameObject); // ✅ ลบที่นี่บน Server
                        }
                        break;

                    case ItemType.UnlimitedAmmo:  // ถ้าเป็นประเภท UnlimitedAmmo
                        if (playerNetworkObject.TryGetComponent<GunController>(out var gunControllerUnlimited))
                        {
                            gunControllerUnlimited.ActivateUnlimitedAmmo();  // เรียกใช้ฟังก์ชันนี้
                            Destroy(gameObject); // ✅ ลบที่นี่บน Server
                        }
                        break;
                }
            }
        }
    }

    [ClientRpc]
    private void AttemptUnlockGunClientRpc(NetworkObjectReference playerNetworkObjectReference, string gunName, NetworkObjectReference itemRef)
    {
        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<GunController>(out GunController gunController))
            {
                if (!gunController.HasGun(gunName))
                {
                    gunController.UnlockGun(gunName);
                    // ✅ ปลดล็อกสำเร็จ จึงทำลายไอเทม
                    DestroyItemServerRpc(itemRef);
                }
                else
                {
                    Debug.Log($"Client: Already has gun {gunName}, item not destroyed.");
                }
            }
            else
            {
                Debug.LogWarning("GunController not found on the player.");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve player NetworkObjectReference.");
        }
    }

    [ClientRpc]
    private void RestoreHealthClientRpc(NetworkObjectReference playerNetworkObjectReference, int healthAmount, NetworkObjectReference itemRef)
    {
        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<Health>(out Health playerHealth))
            {
                playerHealth.Heal(healthAmount);
                DestroyItemServerRpc(itemRef);
            }
            else
            {
                Debug.LogWarning("Health component not found on player.");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve player NetworkObjectReference.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
    {
        if (itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject))
        {
            Destroy(itemNetworkObject.gameObject);
        }
        else
        {
            Debug.LogError("Failed to resolve item NetworkObjectReference for destruction.");
        }
    }
}
