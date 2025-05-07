
using UnityEngine;
using Unity.Netcode;

public class NetworkedSound : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    // [SerializeField] private List<AudioClip> soundClips; // ตัวอย่าง List AudioClip

    public override void OnNetworkSpawn()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is not assigned on this GameObject!");
            enabled = false;
        }
    }

    public void PlayLocalSound(int soundIndex)
    {
        AudioClip soundToPlay = GetAudioClip(soundIndex);

        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
        else
        {
            Debug.LogWarning($"Sound with index {soundIndex} not found!");
        }

        if (IsSpawned && IsOwner)
        {
            RequestPlaySoundOthersServerRpc(soundIndex, transform.position, NetworkObjectId); // ส่ง NetworkObjectId ไปด้วย
        }
    }

    [ServerRpc]
    public void RequestPlaySoundOthersServerRpc(int soundIndex, Vector3 position, ulong shooterNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        PlaySoundOthersClientRpc(soundIndex, position, shooterNetworkObjectId);
    }

    [ClientRpc]
    public void PlaySoundOthersClientRpc(int soundIndex, Vector3 position, ulong shooterNetworkObjectId)
    {
        // ตรวจสอบว่าเป็น Object อื่นที่ไม่ใช่คนที่ยิง
        if (NetworkObjectId != shooterNetworkObjectId)
        {
            if (position != default)
            {
                transform.position = position;
            }

            AudioClip soundToPlay = GetAudioClip(soundIndex);

            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
            else
            {
                Debug.LogWarning($"Sound with index {soundIndex} not found!");
            }
        }
    }

    private AudioClip GetAudioClip(int index)
    {
        // ตัวอย่าง: สมมติว่าคุณมี List ของ AudioClip ชื่อ "soundClips"
        // if (soundClips != null && index >= 0 && index < soundClips.Count)
        // {
        //     return soundClips[index];
        // }
        // return null;

        Debug.LogWarning("GetAudioClip function is not implemented!");
        return null;
    }
}