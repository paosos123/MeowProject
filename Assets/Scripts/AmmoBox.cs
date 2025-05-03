using UnityEngine;
using Unity.Netcode;

public class AmmoBox : NetworkBehaviour
{
    [SerializeField] private string gunType; // ประเภทปืนที่กล่องนี้เติม (ตั้งชื่อให้ตรงกับ gunName ใน GunData)
    [SerializeField] private int ammoAmount = 15; // จำนวนกระสุนที่เติม

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ทำงานบน Server เท่านั้นเพื่อป้องกันการทำงานซ้ำซ้อน
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // รับ NetworkObject ของผู้เล่น
            NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                // เรียกฟังก์ชันเพื่อเติมกระสุนและทำลายกล่อง
                PickupAmmo(playerNetworkObject);
            }
        }
    }

    // ฟังก์ชันที่จัดการการเติมกระสุนและทำลายกล่อง
    private void PickupAmmo(NetworkObject playerNetworkObject)
    {
        // ค้นหา GunController จาก NetworkObject ของผู้เล่น
        GunController gunController = playerNetworkObject.GetComponent<GunController>();
        if (gunController != null)
        {
            // เรียก ServerRpc บน GunController เพื่อเติมกระสุนไ
            gunController.AddAmmoServerRpc(gunType, ammoAmount, playerNetworkObject.OwnerClientId); // Pass the ClientId

            // ทำลายกล่องกระสุนหลังจากเก็บ (บน Server)
            Destroy(gameObject);
            if (NetworkObject != null)
            {
                NetworkObject.Despawn(); // Despawn บน Network
            }
        }
    }
}
