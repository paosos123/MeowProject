using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    [SerializeField] private MovementPlayer player;
    [SerializeField] private SpriteRenderer[] playerSprites;

    [SerializeField] private int colorIndex;

    private void Start()
    {
        HandlePlayerColorChanged(0, player.PlayerColorIndex.Value);
        player.PlayerColorIndex.OnValueChanged += HandlePlayerColorChanged;
    }

    private void HandlePlayerColorChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"Color Changed : {newIndex}");
        colorIndex = newIndex;
    }

    private void OnDestroy()
    {
        player.PlayerColorIndex.OnValueChanged -= HandlePlayerColorChanged;
    }
}