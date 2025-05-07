using Unity.Netcode;
using UnityEngine;

public class soundbgmusic : NetworkBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource not found!");
            enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // เริ่มเล่นเพลงบน Server เมื่อ Network Spawn
            PlayMusic();
        }
    }

    private void PlayMusic()
    {
        if (IsServer && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("Background music started playing on Server.");
        }
    }

    private void StopMusic()
    {
        if (IsServer && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Background music stopped playing on Server.");
        }
    }

    // คุณสามารถเพิ่มฟังก์ชันควบคุมอื่นๆ ที่ทำงานบน Server ได้ตามต้องการ
}