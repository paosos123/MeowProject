using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField] private Animator shootingAnim;
    [SerializeField] private List<GameObject> gunPrefabs;
    [SerializeField] private PlayerUIController ui;

    private int currentGunIndex = 0;
    private GameObject currentGunObject;
    private WeaponBehavior currentWeapon;

    void Start()
    {
        if (gunPrefabs.Count > 0)
        {
            EquipGun(0);
        }
    }

    void Update()
    {
        HandleGunSwitching();
        UpdateGunRotation();
        HandleShooting();
    }

    private void EquipGun(int index)
    {
        if (currentGunObject != null)
        {
            Destroy(currentGunObject);
        }

        GameObject gunInstance = Instantiate(gunPrefabs[index], transform);
        currentGunObject = gunInstance;
        currentWeapon = gunInstance.GetComponent<WeaponBehavior>();

        if (currentWeapon == null)
        {
            return;
        }

        currentGunIndex = index;
        currentWeapon.currentAmmo = currentWeapon.maxAmmo;
        currentWeapon.nextFireTime = Time.time;
        UpdateAmmoUI();
    }

    private void HandleGunSwitching()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            int next = (currentGunIndex + 1) % gunPrefabs.Count;
            EquipGun(next);
        }
        else if (scroll < 0f)
        {
            int prev = (currentGunIndex - 1 + gunPrefabs.Count) % gunPrefabs.Count;
            EquipGun(prev);
        }
    }

    private void UpdateGunRotation()
    {
        if (currentWeapon == null || currentWeapon.firePoint == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 dir = mousePos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        currentWeapon.firePoint.rotation = Quaternion.Euler(0, 0, angle);
        currentGunObject.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void HandleShooting()
    {
        if (currentWeapon == null || currentWeapon.firePoint == null) return;

        if (Input.GetMouseButton(0) && Time.time >= currentWeapon.nextFireTime)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (currentWeapon.currentAmmo <= 0) return;

        shootingAnim?.SetTrigger("Shoot");

        for (int i = 0; i < currentWeapon.bulletsPerShot; i++)
        {
            float spread = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
            float angle = currentWeapon.firePoint.eulerAngles.z + spread;

            GameObject bullet = Instantiate(currentWeapon.bulletPrefab);
            bullet.transform.position = currentWeapon.firePoint.position;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
                rb.linearVelocity = dir * currentWeapon.bulletSpeed;
            }
        }

        currentWeapon.currentAmmo -= currentWeapon.bulletsPerShot;
        currentWeapon.nextFireTime = Time.time + currentWeapon.fireRate;
        UpdateAmmoUI();
    }

    public void AddAmmo(string gunName, int amount)
    {
        if (currentWeapon != null && currentWeapon.gunName == gunName)
        {
            currentWeapon.currentAmmo = Mathf.Min(currentWeapon.currentAmmo + amount, currentWeapon.maxAmmo);
            UpdateAmmoUI();
        }
    }

    private void UpdateAmmoUI()
    {
        if (ui != null && currentWeapon != null)
        {
            ui.UpdateAmmo(currentWeapon.currentAmmo, currentWeapon.maxAmmo);
        }
    }

    public void UnlockGun(string gunName)
    {
        for (int i = 0; i < gunPrefabs.Count; i++)
        {
            var weapon = gunPrefabs[i].GetComponent<WeaponBehavior>();
            if (weapon != null && weapon.gunName == gunName)
            {
                EquipGun(i);
                return;
            }
        }
    }
}
