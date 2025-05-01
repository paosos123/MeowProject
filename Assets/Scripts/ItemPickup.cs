using Unity.Netcode;
using UnityEngine;

public class ItemPickup : NetworkBehaviour
{
    public enum ItemType
    {
        Gun,
        Health
    }

    public ItemType itemType;
    public string gunToUnlock;
    public int healthToRestore = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // ให้ Server เป็นผู้จัดการการเก็บไอเทม

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<NetworkObject>(out NetworkObject playerNetworkObject))
            {
                switch (itemType)
                {
                    case ItemType.Gun:
                        // เรียก RPC บน Client ที่เป็นเจ้าของ Player เพื่อตรวจสอบและปลดล็อกปืน
                        AttemptUnlockGunClientRpc(playerNetworkObject, gunToUnlock);
                        break;
                    case ItemType.Health:
                        // เรียก RPC บน Client ที่เป็นเจ้าของ Player เพื่อเพิ่มเลือด
                        RestoreHealthClientRpc(playerNetworkObject, healthToRestore);
                        break;
                    default:
                        Debug.LogWarning("Unknown Item Type!");
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Player GameObject does not have a NetworkObject!");
            }
        }
    }

    [ClientRpc]
    private void AttemptUnlockGunClientRpc(NetworkObjectReference playerNetworkObjectReference, string gunName)
    {
        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<GunController>(out GunController gunController))
            {
                // ตรวจสอบว่า Player มีปืนนี้อยู่แล้วหรือไม่
                if (!gunController.HasGun(gunName))
                {
                    gunController.UnlockGun(gunName);
                    // แจ้งให้ Server ทำลายไอเทม หลังจาก Client ปลดล็อกสำเร็จ
                    DestroyItemServerRpc(NetworkObject);
                }
                else
                {
                    Debug.Log($"Player (ClientId: {playerNetworkObject.OwnerClientId}) already has the gun: {gunName}");
                }
            }
            else
            {
                Debug.LogWarning($"GunController not found on the Player GameObject with NetworkId: {playerNetworkObject.NetworkObjectId}!");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve NetworkObjectReference for the Player!");
        }
    }

    [ClientRpc]
    private void RestoreHealthClientRpc(NetworkObjectReference playerNetworkObjectReference, int healthAmount)
    {
        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<Health>(out Health playerHealth))
            {
                playerHealth.Heal(healthAmount);
                // แจ้งให้ Server ทำลายไอเทม หลังจาก Client เพิ่มเลือดสำเร็จ
                DestroyItemServerRpc(NetworkObject);
            }
            else
            {
                Debug.LogWarning($"Health component not found on the Player GameObject with NetworkId: {playerNetworkObject.NetworkObjectId}!");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve NetworkObjectReference for the Player!");
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
            Debug.LogError("Failed to resolve NetworkObjectReference for the item to destroy!");
        }
    }
}