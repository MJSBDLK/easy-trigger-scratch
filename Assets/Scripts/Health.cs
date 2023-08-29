using UnityEngine;

public class Health : MonoBehaviour
{
    public enum CharacterType { Player, Enemy }
    public CharacterType characterType;
    public int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount, Vector2 hitDirection = default)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Die(hitDirection);
        }
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
            Destroy(gameObject);
        }
    }
}

