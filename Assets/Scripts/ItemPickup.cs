using Unity.Netcode;
using UnityEngine;

public class ItemPickup : NetworkBehaviour
{
    public string gunToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // ให้ Server เป็นผู้จัดการการเก็บไอเทม

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<NetworkObject>(out NetworkObject playerNetworkObject))
            {
                // เรียก RPC บน Client ที่เป็นเจ้าของ Player เพื่อปลดล็อกปืน
                UnlockGunClientRpc(playerNetworkObject, gunToUnlock);
                Destroy(gameObject); // ทำลายไอเทมบน Server
            }
            else
            {
                Debug.LogWarning("Player GameObject does not have a NetworkObject!");
            }
        }
    }

    [ClientRpc]
    private void UnlockGunClientRpc(NetworkObjectReference playerNetworkObjectReference, string gunName)
    {
        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<GunController>(out GunController gunController))
            {
                gunController.UnlockGun(gunName);
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
}