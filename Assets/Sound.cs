using Unity.Netcode;
using UnityEngine;

public class Sound : NetworkBehaviour
{
    [Header("Audio Settings")]
    public AudioSource pickupAudioSource;
    public AudioClip healthItemSound;
    public AudioClip infiAmmoSound;
    public AudioClip ammoBoxSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner || pickupAudioSource == null) return;

        // Check the tag of the object we collided with
        switch (other.tag)
        {
            case "HealthItem":
                pickupAudioSource.PlayOneShot(healthItemSound);
                break;
            case "InfiAmmo":
                pickupAudioSource.PlayOneShot(infiAmmoSound);
                break;
            case "Ammobox":
                pickupAudioSource.PlayOneShot(ammoBoxSound);
                break;
            default:
                return; // Exit if it's not an item we care about
        }
     
       

    }

    private int GetPickupType(string tag)
    {
        switch (tag)
        {
            case "HealthItem":
                return 0;
            case "InfiAmmo":
                return 1;
            case "Ammobox":
                return 2;
            default:
                Debug.LogWarning("Tag ไม่รู้จัก: " + tag);
                return -1; // Handle unknown tag
        }
    }
}