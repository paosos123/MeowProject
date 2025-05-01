using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOverUIController : MonoBehaviour
{
    public static GameOverUIController Instance;
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // ถ้าคุณต้องการให้ UI นี้อยู่รอดข้าม Scene
        }
        else
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

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // ฟังก์ชันสำหรับปุ่ม Restart - เลิกเป็น Host และโหลด Menu
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
    

    // (Optional) ฟังก์ชันสำหรับปุ่ม Exit
    public void ExitGame()
    {
        // Logic สำหรับการออกจากเกม
        Debug.Log("Exit Button Clicked");
        Application.Quit();
    }
}