using Unity.Netcode;
using UnityEngine;

public class CanvasFlipCorrection : NetworkBehaviour
{
    public Movements parentMovement; // ลาก GameObject หลักมาใส่ใน Inspector

    private bool wasFacingRight;

    void Start()
    {
       
        if (parentMovement == null)
        {
            Debug.LogError("Parent Movements script not assigned!");
            enabled = false;
            return;
        }
        wasFacingRight = parentMovement.IsFacingRight;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (parentMovement.IsFacingRight != wasFacingRight)
        {
            transform.Rotate(0, 180, 0);
            wasFacingRight = parentMovement.IsFacingRight;
        }
    }
}