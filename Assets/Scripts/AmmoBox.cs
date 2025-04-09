using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [SerializeField] private string gunType; // ประเภทปืนที่กล่องนี้เติม (ตั้งชื่อให้ตรงกับ gunName ใน GunData)
    [SerializeField] private int ammoAmount = 15; // จำนวนกระสุนที่เติม

    private void OnTriggerEnter2D(Collider2D other)
    {
        GunController gunController = other.GetComponent<GunController>();

        if (gunController != null)
        {
            // พบ Player ที่มี GunController
            gunController.AddAmmo(gunType, ammoAmount);

            // ทำลายกล่องกระสุนหลังจากเก็บแล้ว
            Destroy(gameObject);
        }
    }
}