using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerReady : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Button readyButton; // เชื่อมกับปุ่ม "พร้อม" ใน UI

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            readyButton.onClick.AddListener(SetPlayerReady);
        }
        else
        {
            readyButton.gameObject.SetActive(false); // ซ่อนปุ่มสำหรับ Client อื่น
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            readyButton.interactable = !IsReady.Value; // ทำให้ปุ่มไม่สามารถกดได้ถ้าผู้เล่นคนอื่นพร้อมแล้ว
        }
    }

    [ServerRpc]
    private void SetPlayerReadyServerRpc(bool ready)
    {
        IsReady.Value = ready;

        // ตรวจสอบว่าผู้เล่นทุกคนพร้อมแล้วหรือไม่ (ทำงานบน Server เท่านั้น)
        CheckAllPlayersReady();
    }

    private void SetPlayerReady()
    {
        SetPlayerReadyServerRpc(true);
        readyButton.interactable = false; // ทำให้ปุ่มกดไม่ได้หลังจากกด
    }

    private void CheckAllPlayersReady()
    {
        if (!IsServer) return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager.ConnectedClients.Count > 0 && networkManager.ConnectedClients.Count == networkManager.ConnectedClientsIds.Count)
        {
            bool allReady = true;
            foreach (var client in networkManager.ConnectedClients)
            {
                if (client.Value != null && client.Value.PlayerObject != null)
                {
                    PlayerReady playerReady = client.Value.PlayerObject.GetComponent<PlayerReady>();
                    if (playerReady != null && !playerReady.IsReady.Value)
                    {
                        allReady = false;
                        Debug.Log($"Player {client.Key} is not ready yet.");
                        break; // พบผู้เล่นที่ยังไม่พร้อม
                    }
                }
                else
                {
                    allReady = false;
                    Debug.LogWarning($"Client {client.Key} or their PlayerObject is null!");
                    break;
                }
            }

            if (allReady)
            {
                // ผู้เล่นทุกคนพร้อมแล้ว! เริ่มเกม
                Debug.Log("All players are ready! Starting game...");
                StartGame();
            }
        }
        else
        {
            Debug.Log($"Not all clients are fully connected yet. Connected Clients: {networkManager.ConnectedClients.Count}, Client IDs: {networkManager.ConnectedClientsIds.Count}");
        }
    }

    private void StartGame()
    {
        // Logic สำหรับการเริ่มเกม
        // เช่น เปลี่ยน Scene, เริ่มการทำงานของ Game Manager, ฯลฯ
        // ตัวอย่างการเปลี่ยน Scene:
        // NetworkManager.Singleton.SceneManager.LoadScene("ชื่อ Scene เกม", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}