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
                //  Notify the server that the player wants to pick up the item.
                PickupItemServerRpc(playerNetworkObject, GetComponent<NetworkObject>());
            }
        }
    }

    [ServerRpc(RequireOwnership = false)] // IMPORTANT:  RequireOwnership = false
    private void PickupItemServerRpc(NetworkObjectReference playerNetworkObjectReference, NetworkObjectReference itemNetworkObjectReference)
    {
        if (!playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject))
        {
            Debug.LogError("PickupItemServerRpc: Failed to resolve player NetworkObjectReference.");
            return;
        }

        if (!itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject))
        {
            Debug.LogError("PickupItemServerRpc: Failed to resolve item NetworkObjectReference.");
            return;
        }

        switch (itemType)
        {
            case ItemType.Gun:
                if (playerNetworkObject.TryGetComponent<GunController>(out var gunController))
                {
                    if (!gunController.HasGun(gunToUnlock))
                    {
                        gunController.UnlockGun(gunToUnlock);
                        DestroyItemServerRpc(itemNetworkObjectReference);
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
                  
                    DestroyItemServerRpc(itemNetworkObjectReference);
                }
                break;

            case ItemType.UnlimitedAmmo:
                if (playerNetworkObject.TryGetComponent<GunController>(out var gunControllerUnlimited))
                {
                    gunControllerUnlimited.ActivateUnlimitedAmmo();
                    DestroyItemServerRpc(itemNetworkObjectReference);
                }
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
    {
        if (itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject))
        {
            if (itemNetworkObject != null && itemNetworkObject.gameObject != null)
            {
                Destroy(itemNetworkObject.gameObject);
            }
            else
            {
                Debug.LogError("DestroyItemServerRpc: itemNetworkObject or itemNetworkObject.gameObject is null.");
            }
        }
        else
        {
            Debug.LogError("Failed to resolve item NetworkObjectReference for destruction.");
        }
    }
}

