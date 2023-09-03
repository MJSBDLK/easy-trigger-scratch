using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region Attributes
    [Header("Attributes")]
    public float walkingSpeed = 1f;
    public float detectRange = 5f;
    public float telegraphDuration = 0.5f;  // delay before shooting after telegraphing
    public float shootAnimationDuration = 0.6f;
    public int bodyDamage = 17;
    public int headDamage = 34;  // taking extra damage on head hit
    public int health = 100;
    private float shootingCooldown = 0f; // X seconds cooldown after shooting
    private float lastShootTime;
    private float groundCheckRadius = 0.6f; // Don't fuck with this
    #endregion

    #region State Variables
    private bool isFacingRight = true;
    private bool isShooting = false;
    private bool isDead = false;

    #endregion

    #region Components
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D bodyHurtBox;
    private CircleCollider2D headHurtBox;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject enemyProjectilePrefab;
    #endregion

    #region Layer Masks
    int playerLayer;
    #endregion

    #region Audio
    public AudioSource deathAudioSource;  // Assign this in the inspector
    public AudioClip[] deathSounds;  // Populate this in the inspector with your three sound effects
    #endregion

    [SerializeField] private PlayerMovement playerRef;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyHurtBox = GetComponent<CapsuleCollider2D>();
        headHurtBox = GetComponent<CircleCollider2D>();

        playerLayer = LayerMask.NameToLayer("PlayerHurtBox");
    }

    private void FixedUpdate()
    {
        if (!playerRef.IsDead())
        {
            string stateName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
            // Debug.Log("Animator - Current State: " + stateName);

            animator.SetFloat("horizontalVelocity", Mathf.Abs(rb.velocity.x));
            DetectPlayerAndAct();

        }
        else
        {
            animator.SetTrigger("setIdle");
        }
    }

    private void DetectPlayerAndAct()
    {
        RaycastHit2D hit = Physics2D.Raycast(muzzle.position, isFacingRight ? Vector2.right : Vector2.left, detectRange);

        if (hit.collider != null && hit.collider.gameObject.layer == playerLayer)
        {
            // Debug.Log("Player detected - EXTERMINATE");
            if (!isShooting && (Time.time > (lastShootTime + shootingCooldown)))
            {
                StartCoroutine(TelegraphAndShoot());
            }
        }
        else
        {
            Walk();
        }
    }



    private void Walk()
    {
        if (isShooting) return;  // Don't walk if the enemy is preparing to shoot

        int groundLayer = LayerMask.NameToLayer("Ground");
        int platformLayer = LayerMask.NameToLayer("OneWayPlatform");
        int walkableLayers = (1 << groundLayer) | (1 << platformLayer);

        RaycastHit2D groundHit = Physics2D.Raycast(muzzle.position, Vector2.down, groundCheckRadius, walkableLayers);
        if (groundHit.collider == null)
        {
            Flip();
        }

        rb.velocity = new Vector2(isFacingRight ? walkingSpeed : -walkingSpeed, rb.velocity.y);
    }




    private IEnumerator TelegraphAndShoot()
    {
        if (isDead) yield break;

        isShooting = true;
        animator.SetBool("isShooting", isShooting);
        animator.SetTrigger("telegraph");

        rb.velocity = new Vector2(0, rb.velocity.y);  // Stop horizontal movement

        yield return new WaitForSeconds(telegraphDuration);

        animator.SetTrigger("shoot");

        // Check the enemy's facing direction and set the rotation accordingly
        Quaternion projectileRotation = isFacingRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        if (isDead) yield break;
        if (!isDead) Instantiate(enemyProjectilePrefab, muzzle.position, projectileRotation);

        lastShootTime = Time.time; // Record the time when the enemy shot
        // Debug.Log("Kaboom");

        yield return new WaitForSeconds(shootAnimationDuration);

        isShooting = false;
        animator.SetBool("isShooting", isShooting);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private IEnumerator Flash(Color color, int flashCount = 5)
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

    }

    private IEnumerator FadeThenDestroy()
    {
        // Flash red on lethal damage
        StartCoroutine(Flash(Color.red));

        // Wait for the duration of the flash
        yield return new WaitForSeconds(0.5f); // Assuming flash duration is 0.5 seconds

        // Now begin fading out the sprite
        float duration = 1f;
        float elapsedTime = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsedTime / duration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void HandleDeath(Vector2 hitDirection)
    {
        float impulseStrength = 1.5f;
        if (Vector2.Dot(hitDirection, transform.right) < 0)
        {
            // The hit came from the front
            animator.SetTrigger("fallBackward");
            rb.AddForce(-transform.right * impulseStrength, ForceMode2D.Impulse);  // not working?

        }
        else
        {
            // The hit came from behind
            animator.SetTrigger("fallForward");
            rb.AddForce(transform.right * impulseStrength, ForceMode2D.Impulse);  // not working

        }

        PlayRandomDeathSound();

        StartCoroutine(Flash(Color.red));  // red on lethal damage
        StartCoroutine(FadeThenDestroy());  // <-- Add this line

        // disable any further behavior
        this.enabled = false;
        // GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    private void PlayRandomDeathSound()
    {
        int randomIndex = UnityEngine.Random.Range(0, deathSounds.Length);
        deathAudioSource.clip = deathSounds[randomIndex];
        deathAudioSource.Play();
    }


    private void OnDrawGizmos()
    {
        // Draw floor detection raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(muzzle.position, muzzle.position + new Vector3(0, -groundCheckRadius, 0));

        // Draw player detection raycast
        Gizmos.color = Color.green;
        Vector2 rayStart = muzzle.position;
        Vector2 rayDirection = isFacingRight ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(rayStart, rayStart + rayDirection * detectRange);

        // Draw body hurtbox
        // This isn't working properly but whatever
        Gizmos.color = Color.red;
        Vector3 currentScale = transform.localScale;
        if (bodyHurtBox != null)
        {
            Vector2 bodyPosition = bodyHurtBox.bounds.center;
            Vector2 bodySize = bodyHurtBox.size * currentScale.y; // Adjust for scale
            float radius = (bodySize.x / 2) * currentScale.x;
            float height = bodySize.y - (2 * radius);
            // Debug to check our computed values
            // Debug.Log($"Body Radius: {radius}, Body Height: {height}");

            // Drawing the upper and lower circles
            Gizmos.DrawWireSphere(bodyPosition + Vector2.up * (height / 2), radius);
            Gizmos.DrawWireSphere(bodyPosition - Vector2.up * (height / 2), radius);

            // Drawing the rectangle connecting the two circles
            Gizmos.DrawLine(new Vector2(bodyPosition.x - radius, bodyPosition.y + (height / 2)), new Vector2(bodyPosition.x - radius, bodyPosition.y - (height / 2)));
            Gizmos.DrawLine(new Vector2(bodyPosition.x + radius, bodyPosition.y + (height / 2)), new Vector2(bodyPosition.x + radius, bodyPosition.y - (height / 2)));
        }

        // Draw head hurtbox
        Gizmos.color = Color.blue;
        if (headHurtBox != null)
        {
            Vector3 headPosition = headHurtBox.bounds.center;
            float headRadius = headHurtBox.bounds.extents.magnitude;  // Not a true radius, but should give a reasonable size
            Gizmos.DrawWireSphere(headPosition, headRadius);
        }

    }

}

