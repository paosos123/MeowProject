using Unity.Netcode;

using UnityEngine;



public class CanvasFlipCorrection : NetworkBehaviour

{

    public Movements parentMovement; // ลาก GameObject หลักมาใส่ใน Inspector

    private Vector3 initialScale;

    private bool wasFacingRight;



    void Start()

    {

        if (!IsOwner) return;



        if (parentMovement == null)

        {

            Debug.LogError("Parent Movements script not assigned!");

            enabled = false;

            return;

        }

        initialScale = transform.localScale;

        wasFacingRight = parentMovement.IsFacingRight;

    }



    void Update()

    {
        if (!IsOwner) return;
        if (parentMovement.IsFacingRight != wasFacingRight)

        {

            transform.localScale = new Vector3(initialScale.x * (parentMovement.IsFacingRight ? 1f : -1f), initialScale.y, initialScale.z);

            wasFacingRight = parentMovement.IsFacingRight;

        }

    }

}