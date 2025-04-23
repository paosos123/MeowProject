using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Movements : NetworkBehaviour
{
    private Rigidbody2D rb; // เปลี่ยนเป็น NetworkRigidbody2D
    private bool facingRight = true;
    [SerializeField] float moveSpeed = 10f;
    public bool IsFacingRight => facingRight;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // GetComponent เป็น NetworkRigidbody2D
    }

    void Update()
    {
        if(!IsOwner)
            return;
        Move();
        FlipController();
    }

    private void FlipController()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x && facingRight)
            Flip();
        else if (mousePos.x > transform.position.x && !facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
    }



    void Move()
    {
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveInputX, moveInputY).normalized;

        // ใช้ velocity ของ NetworkRigidbody2D ในการเคลื่อนที่
        if (rb != null)
        {
            rb.linearVelocity= movement * moveSpeed;   // ใช้ Velocity ของ NetworkRigidbody2D
        }
    }
}