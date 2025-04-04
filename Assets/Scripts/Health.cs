using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHp = 100;
    private int currentHp;

    [Header("Lives Settings")]
    public int maxLives = 3;
    private int currentLives;

    [Header("Spawn Settings")]
    public Vector3 startPosition; // กำหนดตำแหน่งเริ่มต้นใน Inspector

    [Header("HealthBarFill Settings")] 
    public Image healthBarFill;
    
    [Header("HealthBarFill Settings")] 
    [SerializeField] private int damage =20;
    
    bool isHit = false;
    void Start()
    {
        // กำหนดค่าเริ่มต้นเมื่อเกมเริ่ม
        currentHp = maxHp;
        currentLives = maxLives;
        startPosition = transform.position; // บันทึกตำแหน่งเริ่มต้นของ GameObject
        Debug.Log("ตัวละครเกิดใหม่ HP: " + currentHp + ", ชีวิต: " + currentLives);
        UpdateHealthBarFill();
    }
    // ตัวอย่างวิธีใช้งาน (สามารถเรียกใช้จาก Script อื่นได้)
    void Update()
    {
        
        // ตัวอย่าง: กดปุ่ม Spacebar เพื่อจำลองการได้รับความเสียหาย 20 หน่วย
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20);
        }
    }

    // ฟังก์ชันสำหรับรับความเสียหาย
    public void TakeDamage(int damage)
    {
        if (currentHp <= 0|| isHit) // ป้องกันการรับความเสียหายเมื่อตายแล้ว
        {
            return;
        }

        currentHp -= damage;
        UpdateHealthBarFill();
        Debug.Log("ได้รับความเสียหาย: " + damage + ", HP ปัจจุบัน: " + currentHp);
        StartCoroutine(GetHurt());

        // ตรวจสอบว่าตายหรือไม่
        if (currentHp <= 0)
        {
            Die();
        }
    }

    // ฟังก์ชันเมื่อตัวละครตาย
    void Die()
    {
        Debug.Log("ตัวละครตาย!");
        currentLives--;

        // ตรวจสอบว่ายังมีชีวิตเหลืออยู่หรือไม่
        if (currentLives > 0)
        {
            Respawn();
        }
        else
        {
            GameOver();
        }
    }

    // ฟังก์ชันเกิดใหม่
    void Respawn()
    {
        currentHp = maxHp;
        transform.position = startPosition; // ย้ายตัวละครกลับไปยังตำแหน่งเริ่มต้น
        Debug.Log("ตัวละครเกิดใหม่ HP: " + currentHp + ", ชีวิตที่เหลือ: " + currentLives);
    }

    // ฟังก์ชันเมื่อชีวิตหมด
    void GameOver()
    {
        Debug.Log("Game Over! ไม่มีชีวิตเหลือแล้ว");
        // คุณสามารถใส่ Logic เพิ่มเติมสำหรับการจัดการ Game Over ได้ที่นี่
    }

    void UpdateHealthBarFill()
    {
        healthBarFill.fillAmount = (float)currentHp /(float)maxHp;
    }

    IEnumerator GetHurt()
    {
        Physics2D.IgnoreLayerCollision(7,8);
        GetComponent<Animator>().SetLayerWeight(1,1);
        isHit = true;
        yield return new WaitForSeconds(2);
        GetComponent<Animator>().SetLayerWeight(1,0);
        Physics2D.IgnoreLayerCollision(7,8,false);
        isHit = false;
        if (currentHp <= 0)
        {
            Die();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.tag)
        {
            case "Enemy":
                TakeDamage(damage);
                break;
            case "EnemyBullet":
                TakeDamage(damage);
                break;
            // คุณสามารถเพิ่ม case อื่นๆ สำหรับ Tag อื่นๆ ได้ที่นี่
            // case "Enemy":
            //     Debug.Log("ชนศัตรู");
            //     break;
            default:
                // กรณีที่ Tag ไม่ตรงกับ case ใดๆ
                // Debug.Log("ชนวัตถุอื่น: " + other.tag);
                break;
        }
    }
    
}
