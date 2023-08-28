using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseProjectile : MonoBehaviour
{
    public enum ProjectileType { PlayerProjectile, EnemyProjectile };

    [Header("Attributes")]
    public float speed = 5f;
    public int damage = 17;
    public ProjectileType projectileType;


    [Header("Components")]
    public Transform visual;

    protected float rotationSpeed;

    private void Start()
    {
        InitializeProjectile();
        Destroy(gameObject, 10f);
    }

    protected virtual void InitializeProjectile()
    {
        rotationSpeed = Random.Range(-720, 720);
    }

    private void Update()
    {
        MoveProjectile();
        RotateVisual();
    }

    private void MoveProjectile()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime, Space.Self);
    }

    private void RotateVisual()
    {
        visual.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        string layerName = LayerMask.LayerToName(other.gameObject.layer);

        if (projectileType == ProjectileType.PlayerProjectile && layerName == "Enemy")
        {
            // Apply damage to the enemy
            Destroy(gameObject);  // Destroy the projectile
        }
        else if (projectileType == ProjectileType.EnemyProjectile && layerName == "PlayerHurtBox")
        {
            // Damage the player
            Destroy(gameObject);  // Destroy the projectile
        }
    }

}

