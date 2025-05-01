using UnityEngine;

public class LockYRotation : MonoBehaviour
{
    void Update()
    {
          Vector3 currentRotation = transform.eulerAngles;
          transform.eulerAngles = new Vector3(currentRotation.x, 0f, currentRotation.z);
    }
}