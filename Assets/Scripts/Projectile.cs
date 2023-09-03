using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum ProjectileType { PlayerProjectile, EnemyProjectile };

    [Header("Attributes")]
    public float speed = 5f;
    public int damage = 17;
    public ProjectileType projectileType;
    public ParticleSystem particleEffect;


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
        if (particleEffect != null)
        {
            InstantiateParticleEffect();
        }
    }

    private void InstantiateParticleEffect()
    {
        if (particleEffect != null)
        {
            ParticleSystem instantiatedEffect = Instantiate(particleEffect, transform.position, Quaternion.identity);

            // Get the main module of the particle system to access its duration.
            ParticleSystem.MainModule mainModule = instantiatedEffect.main;
            float particleDuration = mainModule.duration + mainModule.startLifetime.constantMax;

            // Destroy the particle system after its full duration.
            Destroy(instantiatedEffect.gameObject, particleDuration);
        }
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

    // private void PlayParticleEffectOnDestroy()
    // {
    //     if (particleEffect != null)
    //     {
    //         Instantiate(particleEffect, transform.position, Quaternion.identity);
    //     }
    // }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Health targetHealth = other.GetComponent<Health>();
        Vector2 hitDirection = other.transform.position - transform.position;

        if (targetHealth != null)
        {
            // Check for friendly fire or any other condition if needed
            targetHealth.TakeDamage(damage, hitDirection.normalized);
            InstantiateParticleEffect();
            Destroy(gameObject);
        }
    }

}

