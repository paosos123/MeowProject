using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOverUIController : NetworkBehaviour
{
   
    public static GameOverUIController Instance { get; private set; }

    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false); // ซ่อน Panel ตอนเริ่มเกม
        }
        else
        {
            Debug.LogError("GameOver Panel ไม่ได้ถูกกำหนดใน Inspector!");
        }
    }

    [ClientRpc]
    public void ShowGameOverClientRpc(ulong clientId) // รับ ClientId ของผู้เล่นที่ตาย
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) // ตรวจสอบว่าเป็น Client ของผู้เล่นที่ตายหรือไม่
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true); // แสดง UI GameOver บน Client ที่ถูกต้องเท่านั้น
            }
        }
    }

    public void RestartGame()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Stopping Host...");
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Menu");
            }
            else
            {
                Debug.Log("Stopping Client...");
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Menu");
            }
        }
        else
        {
            Debug.LogWarning("NetworkManager Singleton is null. Cannot stop network.");
            SceneManager.LoadScene("Menu");
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exit Button Clicked");
        Application.Quit();
    }
}