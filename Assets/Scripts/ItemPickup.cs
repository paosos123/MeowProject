using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string gunToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GunController gunController = other.GetComponent<GunController>();

            if (gunController != null)
            {
                gunController.UnlockGun(gunToUnlock);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("GunController not found on the Player GameObject!");
            }
        }
    }
}