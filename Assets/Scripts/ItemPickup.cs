using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string gunToUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ค้นหา GunController ใน GameObject ของผู้เล่นที่เข้ามาชน
            GunController gunController = other.GetComponent<GunController>();

            // ตรวจสอบว่าพบ GunController หรือไม่
            if (gunController != null)
            {
                gunController.UnlockGun(gunToUnlock);
                Destroy(gameObject); // ทำลายไอเทมที่เก็บแล้ว
            }
            else
            {
                Debug.LogWarning("GunController not found on the Player GameObject!");
            }
        }
    }
}