using Unity.Netcode;
using UnityEngine;


public class MovementPlayer :  NetworkBehaviour
{
   
    private Rigidbody2D rb; // เพิ่มตัวแปร Rigidbody2D
    [SerializeField] float moveSpeed = 10f;

    void Start()
    {
        // GetComponent Rigidbody2D เมื่อ GameObject ถูกสร้าง
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on this GameObject!");
            enabled = false; // ปิดการทำงานของสคริปต์ถ้าไม่มี Rigidbody2D
        }
    }
    void Update() // ใช้ FixedUpdate สำหรับการเคลื่อนที่ที่เกี่ยวข้องกับฟิสิกส์
    {
        if(!IsOwner)
            return;
        Move();
        
    }
  /*  void FixedUpdate() // ใช้ FixedUpdate สำหรับการเคลื่อนที่ที่เกี่ยวข้องกับฟิสิกส์
    {
        if(!IsOwner)
            return;
        Move();
        
    }*/

    void Move()
    {
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveInputX, moveInputY).normalized;

        // ใช้ velocity ของ Rigidbody2D ในการเคลื่อนที่
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }
}
