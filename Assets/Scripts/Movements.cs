using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Movements : NetworkBehaviour
{
    private Rigidbody2D rb;
    private bool facingRight = true;
    [SerializeField] float moveSpeed = 10f;
    public bool canMove = true;
    public bool IsFacingRight => facingRight;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    [SerializeField] private TMP_Text playerNameText;
    private Animator animator;
    private int isWalkingHash;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform objectToFlip;

    // New NetworkVariable to track facing direction
    public NetworkVariable<bool> FacingRightState = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on the Player GameObject!");
        }

        isWalkingHash = Animator.StringToHash("isWalking");

        if (GetComponent<NetworkAnimator>() == null)
        {
            Debug.LogError("NetworkAnimator component not found on the Player GameObject! Please add it and link the Animator.");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on the Player GameObject!  Please add a SpriteRenderer component.");
        }

        if (objectToFlip == null)
        {
            Debug.LogWarning("objectToFlip is not assigned!  Please assign the Transform of the object you want to flip in the Inspector.");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize on the server.  This is the source of truth.
            FacingRightState.Value = facingRight;
        }

        if (IsOwner)
        {
            FixedString32Bytes playerName = "Player_" + OwnerClientId;
            SetPlayerNameServerRpc(playerName);
        }

        if (playerNameText != null)
        {
            UpdatePlayerNameUI(PlayerName.Value);
        }

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        FacingRightState.OnValueChanged += OnFacingRightStateChanged; //listen to changes
        OnFacingRightStateChanged(facingRight, FacingRightState.Value); //set the initial value
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (playerNameText != null)
        {
            UpdatePlayerNameUI(newValue);
        }
    }
    private void OnFacingRightStateChanged(bool previousValue, bool newValue)
    {
        facingRight = newValue;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !newValue;
        }

        if (objectToFlip != null)
        {
            Vector3 originalLocalPosition = objectToFlip.localPosition;
            objectToFlip.localScale = new Vector3(newValue ? Mathf.Abs(objectToFlip.localScale.x) : -Mathf.Abs(objectToFlip.localScale.x), objectToFlip.localScale.y, objectToFlip.localScale.z);
            objectToFlip.localPosition = new Vector3(newValue ? Mathf.Abs(originalLocalPosition.x) : -Mathf.Abs(originalLocalPosition.x), objectToFlip.localPosition.y, objectToFlip.localPosition.z);
        }
    }

    private void UpdatePlayerNameUI(FixedString32Bytes name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name.ToString();
        }
    }

    void Update()
    {
        if (!IsOwner || !canMove)
            return;
        Move();
        FlipController();
    }

    private void FlipController()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x && facingRight)
            Flip(false);
        else if (mousePos.x > transform.position.x && !facingRight)
            Flip(true);
    }

    private void Flip(bool newFacingRight)
    {
        if (facingRight != newFacingRight)
        {
            facingRight = newFacingRight;
            FlipServerRpc(newFacingRight); //send to server
        }
    }

    [ServerRpc]
    private void FlipServerRpc(bool newFacingRight)
    {
        // Update the NetworkVariable on the server.  This will then be synced to all clients.
        FacingRightState.Value = newFacingRight;
    }

    void Move()
    {
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveInputX, moveInputY).normalized;

        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }

        if (animator != null && IsOwner)
        {
            bool isMoving = movement != Vector2.zero;
            animator.SetBool(isWalkingHash, isMoving);
        }
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(FixedString32Bytes name, ServerRpcParams rpcParams = default)
    {
        PlayerName.Value = name;
    }
}
