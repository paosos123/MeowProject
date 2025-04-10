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
    public Vector3 startPosition;

    [Header("UI Reference")]
    [SerializeField] private PlayerUIController ui;
    [SerializeField] private int damage = 20;

    private bool isHit = false;

    void Start()
    {
        currentHp = maxHp;
        currentLives = maxLives;
        startPosition = transform.position;
        UpdateHealthBarFill();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20);
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHp <= 0 || isHit) return;

        currentHp -= damage;
        UpdateHealthBarFill();
        StartCoroutine(GetHurt());

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        currentLives--;

        if (currentLives > 0)
        {
            Respawn();
        }
        else
        {
            GameOver();
        }
    }

    void Respawn()
    {
        currentHp = maxHp;
        transform.position = startPosition;
        UpdateHealthBarFill();
    }

    void GameOver()
    {
        Debug.Log("Game Over! ไม่มีชีวิตเหลือแล้ว");
        gameObject.SetActive(false);
    }

    void UpdateHealthBarFill()
    {
        if (ui != null)
        {
            ui.UpdateHealthBar(currentHp, maxHp);
        }
    }

    IEnumerator GetHurt()
    {
        Physics2D.IgnoreLayerCollision(7, 8);
        GetComponent<Animator>().SetLayerWeight(1, 1);
        isHit = true;
        yield return new WaitForSeconds(2);
        GetComponent<Animator>().SetLayerWeight(1, 0);
        Physics2D.IgnoreLayerCollision(7, 8, false);
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
            case "EnemyBullet":
                TakeDamage(damage);
                break;
            default:
                break;
        }
    }
}