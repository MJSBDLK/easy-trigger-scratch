using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 17;
    public Transform visual; // This is the child GameObject (original projectile)

    private float rotationSpeed; // rotation speed in degrees per second

    private void Start()
    {
        // Assign a random rotation speed between 0 (no rotation) and 720 (2 full rotations per second)
        rotationSpeed = Random.Range(-720, 720);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime, Space.Self);
        visual.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime); // Only the visual spins
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject);
    }
}
