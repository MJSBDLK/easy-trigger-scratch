using UnityEngine;

public class Health : MonoBehaviour
{
    public enum CharacterType {Player, Enemy}
    public CharacterType characterType;
    public int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (characterType == CharacterType.Enemy) {
            // Play death animation
            // Despawn
        } else if (characterType == CharacterType.Player) {
            // Play death animation
            // Game over
        }

        Destroy(gameObject); // As a simple placeholder.
    }
}
