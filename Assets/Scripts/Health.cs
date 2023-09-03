using Unity.VisualScripting;
using UnityEngine;

public class Health : MonoBehaviour
{
    public enum CharacterType { Player, Enemy }
    public CharacterType characterType;
    public int maxHealth = 100;
    public Color damageFlashColor = Color.red; // Color to flash when taking damage
    public float flashDuration = 0.1f; // Duration of the flash effect

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    // private Coroutine flashCoroutine;


    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] damageSounds;

    private void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public void TakeDamage(int damageAmount, Vector2 hitDirection = default)
    {
        currentHealth -= damageAmount;

        if (characterType == CharacterType.Player)
        {
            PlayDamageSound();
        }

        if (currentHealth <= 0)
        {
            Die(hitDirection);
        }
        else
        {
            DamageFeedback();
        }
    }

    private void DamageFeedback()
    {
        StartCoroutine(FlashSprite());
    }

    private System.Collections.IEnumerator FlashSprite()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = Color.white;
    }

    private void Die(Vector2 hitDirection = default)
    {
        if (characterType == CharacterType.Enemy)
        {
            Enemy enemyComponent = GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.HandleDeath(hitDirection);
            }
            else
            {
                Destroy(gameObject); // Default behavior if no enemy component
            }
        }
        else if (characterType == CharacterType.Player)
        {
            // Play player death animation
            // Game over
            PlayerMovement pm = GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.HandleDeath(hitDirection);
            }
        }
    }

    public void PlayDamageSound()
    {
        if (damageSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, damageSounds.Length);
            audioSource.clip = damageSounds[randomIndex];
            audioSource.Play();
        }
    }
}

