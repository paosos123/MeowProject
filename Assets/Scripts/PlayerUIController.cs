using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image healthBarFill;

    public void SetPlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void UpdateHealthBar(int currentHp, int maxHp)
    {
        healthBarFill.fillAmount = (float)currentHp / maxHp;
    }

    public void UpdateAmmo(int currentMag, int totalAmmo)
    {
        ammoText.text = $"{currentMag} / {totalAmmo}";
    }
}
