using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    public string gunName;
    public Sprite gunSprite;
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    public float bulletSpeed = 10f;
    public int bulletsPerShot = 1;
    public float spreadAngle = 0f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public float nextFireTime;

    public Transform firePoint;
}
