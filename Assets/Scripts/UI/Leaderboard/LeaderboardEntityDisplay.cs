using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    private FixedString32Bytes displayName;

    public int TeamIndex { get; private set; }
    public ulong CliendId { get; private set; }

    public int Coins { get; private set; }

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int coins)
    {
        CliendId = clientId;
        this.displayName = playerName;

        UpdateCoins(coins);
    }
    public void Initialise(int teamIndex, FixedString32Bytes playerName, int coins)
    {
        TeamIndex = teamIndex;
        this.displayName = playerName;

        UpdateCoins(coins);
    }

    public void SetColor(Color color)
    {
        displayText.color = color;
    }

    public void UpdateCoins(int coins)
    {
        Coins = coins;
        UpdateText();
    }

    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {displayName} ({Coins})";
    }
}