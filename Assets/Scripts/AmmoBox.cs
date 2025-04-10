using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [SerializeField] private string gunType;
    [SerializeField] private int ammoAmount = 15;

    private void OnTriggerEnter2D(Collider2D other)
    {
        GunController gunController = other.GetComponent<GunController>();

        if (gunController != null)
        {
            gunController.AddAmmo(gunType, ammoAmount);

            Destroy(gameObject);
        }
    }
}