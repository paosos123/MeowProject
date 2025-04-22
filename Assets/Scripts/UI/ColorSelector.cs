using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SelectionButton
{
    public Image colorButton;
    public GameObject selectionBox;
    public Color color;
}

public class ColorSelector : MonoBehaviour
{
    [SerializeField] private TeamColorLookup colorLookup;
    [SerializeField] private SpriteRenderer[] playerSprites;
    [SerializeField] private SelectionButton[] selectionButton;
    [SerializeField] private int colorIndex = 0;

    public const string PlayerColorKey = "PlayerColorIndex";

    private void OnValidate()
    {
        for (int i = 0; i < selectionButton.Length; i++)
        {
            selectionButton[i].color = (Color)colorLookup.GetTeamColor(i);
        }
        foreach (SelectionButton selection in selectionButton)
        {
            selection.colorButton.color = selection.color;
        }
    }

    private void Start()
    {
        colorIndex = PlayerPrefs.GetInt(PlayerColorKey, 0);
        HandleColorChanged();
    }

    public void HandleColorChanged()
    {
        foreach (SelectionButton selection in selectionButton)
        {
            selection.selectionBox.SetActive(false);
        }
        foreach (SpriteRenderer sprite in playerSprites)
        {
            sprite.color = selectionButton[colorIndex].color;
        }
        selectionButton[colorIndex].selectionBox.SetActive(true);
    }

    public void SelectColor(int colorIndex)
    {
        this.colorIndex = colorIndex;
        HandleColorChanged();
    }

    public void SaveColor()
    {
        PlayerPrefs.SetInt(PlayerColorKey, colorIndex);
    }
}