using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private SpriteRenderer[] playerSprites; // อ้างอิง SpriteRenderer ของตัวละคร

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> PlayerColorIndex = new NetworkVariable<int>(); // NetworkVariable สำหรับเก็บ Index สีผู้เล่น

    public const string PlayerColorKey = "PlayerColorIndex"; // Key สำหรับบันทึก Index สีใน PlayerPrefs

    // Array ของสีที่เลือกได้ (ต้องตรงกับ ColorSelector) - กำหนดใน Inspector
    [SerializeField] private Color[] availableColors;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // โหลดชื่อจาก PlayerPrefs
            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Guest");
            SetNameServerRpc(playerName);

            // โหลด Index สีจาก PlayerPrefs
            int savedColorIndex = PlayerPrefs.GetInt(PlayerColorKey, 0);
            SetColorIndexServerRpc(savedColorIndex);
        }

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        PlayerColorIndex.OnValueChanged += OnPlayerColorIndexChanged;

        UpdatePlayerNameUI(PlayerName.Value);
        UpdatePlayerColor(PlayerColorIndex.Value);
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdatePlayerNameUI(newValue);
    }

    private void UpdatePlayerNameUI(FixedString32Bytes name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name.ToString();
        }
    }

    private void OnPlayerColorIndexChanged(int previousValue, int newValue)
    {
        UpdatePlayerColor(newValue);
    }

    private void UpdatePlayerColor(int colorIndex)
    {
        if (playerSprites != null && availableColors.Length > colorIndex && colorIndex >= 0)
        {
            foreach (SpriteRenderer sprite in playerSprites)
            {
                sprite.color = availableColors[colorIndex];
            }
        }
        else
        {
            Debug.LogWarning($"SpriteRenderer บน {gameObject.name} ไม่พบ หรือ Color Index ไม่ถูกต้อง!");
        }
    }

    [ServerRpc]
    private void SetNameServerRpc(string name)
    {
        PlayerName.Value = name;
        Debug.Log($"Server: Set player name to {name} for ClientId {OwnerClientId}");
    }

    [ServerRpc]
    private void SetColorIndexServerRpc(int colorIndex)
    {
        PlayerColorIndex.Value = colorIndex;
        Debug.Log($"Server: Set player color index to {colorIndex} for ClientId {OwnerClientId}");
    }
}