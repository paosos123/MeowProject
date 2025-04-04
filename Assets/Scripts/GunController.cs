using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using TMPro;

[System.Serializable]
public class GunData
{
    public string gunName;
    public Sprite gunSprite;
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    public float bulletSpeed = 10f;
    public float shootingDistance = 1.5f;
    public bool isUnlocked = false;
    public int bulletsPerShot = 5; // จำนวนกระสุนต่อการยิง
    public float spreadAngle = 15f; // มุมกระจายของกระสุน (องศา)
    public int maxAmmo = 30; // จำนวนกระสุนสูงสุด
    public int currentAmmo; // จำนวนกระสุนปัจจุบัน
    public float nextFireTime; // เวลาที่สามารถยิงนัดถัดไปได้
}

public class GunController : MonoBehaviour
{
    [SerializeField] private Animator shootingAnim;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform bulletTranform;
    [SerializeField] private SpriteRenderer gunRenderer;
    [SerializeField] private List<GunData> gunsData = new List<GunData>();
    [SerializeField] private TMP_Text ammoText;
    private int currentGunIndex = 0;
    private GunData currentGunData;

    void Start()
    {
       
        if (gunsData.Count > 0 && gunRenderer != null)
        {
            gunsData[0].isUnlocked = true;
            gunsData[0].currentAmmo = gunsData[0].maxAmmo;
            SwitchGun(0);
            UpdateAmmoUI(); // เพิ่มบรรทัดนี้
        }
        else
        {
            if (gunsData.Count == 0)
            {
                Debug.LogWarning("No guns configured in the GunController!");
            }
            if (gunRenderer == null)
            {
                Debug.LogError("Gun Renderer not assigned in the Inspector!");
            }
        }
        
    }
    void Update()
    {
       
        ShootingAndMovePoint();
        HandleGunSwitching();
    }

    private void SwitchGun(int index)
    {
        if (index >= 0 && index < gunsData.Count && gunRenderer != null && gunsData[index].isUnlocked)
        {
            currentGunIndex = index;
            currentGunData = gunsData[currentGunIndex];
            gunRenderer.sprite = currentGunData.gunSprite;
           // currentGunData.currentAmmo = currentGunData.maxAmmo; // กำหนดกระสุนปัจจุบันเมื่อสลับปืน
            currentGunData.nextFireTime = Time.time; // รีเซ็ตเวลาการยิงเมื่อเปลี่ยนปืน
            // อัปเดต UI แสดงจำนวนกระสุน
            UpdateAmmoUI();
            Debug.Log(currentGunData.gunName + " loaded. Ammo: " + currentGunData.currentAmmo + "/" + currentGunData.maxAmmo);
        }
        else if (index >= 0 && index < gunsData.Count && !gunsData[index].isUnlocked)
        {
            Debug.Log("You have not picked up the " + gunsData[index].gunName + " yet!");
        }
        else
        {
            Debug.LogError("Invalid gun index or Gun Renderer not assigned!");
        }
       

       
    }
    private void HandleGunSwitching()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput > 0f)
        {
            int nextIndex = (currentGunIndex + 1) % gunsData.Count;
            int startIndex = nextIndex;
            while (!gunsData[nextIndex].isUnlocked)
            {
                nextIndex = (nextIndex + 1) % gunsData.Count;
                if (nextIndex == startIndex)
                {
                    break;
                }
            }
            if (gunsData[nextIndex].isUnlocked)
            {
                SwitchGun(nextIndex);
            }
        }
        else if (scrollInput < 0f)
        {
            int previousIndex = (currentGunIndex - 1 + gunsData.Count) % gunsData.Count;
            int startIndex = previousIndex;
            while (!gunsData[previousIndex].isUnlocked)
            {
                previousIndex = (previousIndex - 1 + gunsData.Count) % gunsData.Count;
                if (previousIndex == startIndex)
                {
                    break;
                }
            }
            if (gunsData[previousIndex].isUnlocked)
            {
                SwitchGun(previousIndex);
            }
        }
    }

    public void UnlockGun(string gunName)
    {
        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == gunName)
            {
                gunsData[i].isUnlocked = true;
                gunsData[i].currentAmmo = gunsData[i].maxAmmo;
                Debug.Log("Picked up the " + gunName + "!");
                SwitchGun(i);
                return;
            }
        }
        Debug.LogWarning("Gun with name " + gunName + " not found!");
    }

    public void Shoot(float angle)
    {
        if (currentGunData == null || currentGunData.currentAmmo <= 0)
        {
            if (currentGunData != null && currentGunData.currentAmmo <= 0)
            {
                Debug.Log("Out of ammo for " + currentGunData.gunName + "!");
            }
            return; // ไม่อนุญาตให้ยิงถ้าไม่มีกระสุน
        }

        if (shootingAnim != null)
        {
            shootingAnim.SetTrigger("Shoot");
        }

        // ยิงกระสุนหลายนัดพร้อมกัน
        for (int i = 0; i < currentGunData.bulletsPerShot; i++)
        {
            // คำนวณมุมของกระสุนแต่ละนัด โดยมีการกระจาย
            float bulletAngle = angle + Random.Range(-currentGunData.spreadAngle, currentGunData.spreadAngle);

            GameObject bulletClone = Instantiate(currentGunData.bulletPrefab);
            bulletClone.transform.position = bulletTranform.position;
            bulletClone.transform.rotation = Quaternion.Euler(0, 0, bulletAngle);

            Rigidbody2D bulletRb = bulletClone.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                Vector2 fireDirection = Quaternion.Euler(0, 0, bulletAngle) * Vector2.right;
                bulletRb.linearVelocity = fireDirection * currentGunData.bulletSpeed;
            }
            else
            {
                Debug.LogError("Bullet prefab does not have a Rigidbody2D component!");
            }
        }

        currentGunData.currentAmmo -= currentGunData.bulletsPerShot; // ลดจำนวนกระสุนที่ยิงไป
        Debug.Log(currentGunData.gunName + " fired. Remaining ammo: " + currentGunData.currentAmmo);
        currentGunData.nextFireTime = Time.time + currentGunData.fireRate; // กำหนดเวลาที่สามารถยิงนัดถัดไปได้
        
        // อัปเดต UI แสดงจำนวนกระสุน
        UpdateAmmoUI();
    }


    private void ShootingAndMovePoint()
    {
        if (currentGunData == null || shootingPoint == null || bulletTranform == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        shootingPoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        shootingPoint.position = transform.position + Quaternion.Euler(0, 0, angle) * new Vector3(currentGunData.shootingDistance, 0, 0);


        bulletTranform.rotation = Quaternion.Euler(0, 0, angle);

        if (Input.GetMouseButton(0) && (Time.time >= currentGunData.nextFireTime))
        {
            Shoot(angle);
        }
    }
    public void AddAmmo(string gunName, int amount)
    {
        for (int i = 0; i < gunsData.Count; i++)
        {
            if (gunsData[i].gunName == gunName)
            {
                gunsData[i].currentAmmo = Mathf.Min(gunsData[i].currentAmmo + amount, gunsData[i].maxAmmo);
                Debug.Log(gunsData[i].gunName + " ammo added. Current ammo: " + gunsData[i].currentAmmo + "/" + gunsData[i].maxAmmo);
                UpdateAmmoUI(); // เพิ่มบรรทัดนี้
                return;
            }
        }
        Debug.LogWarning("Gun with name " + gunName + " not found for ammo refill!");
    }
    private void UpdateAmmoUI()
    {
        if (ammoText != null && currentGunData != null)
        {
            ammoText.text = currentGunData.currentAmmo + " / " + currentGunData.maxAmmo;
        }
        else if (ammoText == null)
        {
            Debug.LogWarning("Ammo Text (TMP_Text) not assigned in the Inspector!");
        }
    }
}