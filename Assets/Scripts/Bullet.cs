using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float timer;
    private Rigidbody2D rb => GetComponent<Rigidbody2D>();
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        transform.right = rb.linearVelocity;
        timer += Time.deltaTime;
        if(timer>=1.5)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Obstacle")
        {
            Destroy(gameObject);
        }
    }
}
