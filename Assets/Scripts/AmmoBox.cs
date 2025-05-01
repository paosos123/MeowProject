using UnityEngine;
using Unity.Netcode;

public class AmmoBox : NetworkBehaviour
{
    [SerializeField] private string gunType; // ประเภทปืนที่กล่องนี้เติม (ตั้งชื่อให้ตรงกับ gunName ใน GunData)
    [SerializeField] private int ammoAmount = 15; // จำนวนกระสุนที่เติม

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // ทำงานเฉพาะบน Server

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.IsOwner)
            {
                // พบ Player ที่เป็นเจ้าของ
                GunController gunController = other.GetComponent<GunController>();
                if (gunController != null)
                {
                    // เรียก ServerRpc บน GunController เพื่อเติมกระสุน
                    gunController.AddAmmoServerRpc(gunType, ammoAmount);

                    // ทำลายกล่องกระสุนหลังจากเก็บแล้ว (บน Server)
                    Destroy(gameObject);
                    if (NetworkObject != null)
                    {
                        NetworkObject.Despawn(); // Despawn บน Network
                    }
                }
            }
        }
    }
}