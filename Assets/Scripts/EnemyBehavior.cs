using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region Attributes
    [Header("Attributes")]
    public float walkingSpeed = 2f;
    public float detectRange = 5f;
    public float telegraphDuration = 1f;  // delay before shooting after telegraphing
    public float shootAnimationDuration = 1f;
    public int bodyDamage = 17;
    public int headDamage = 34;  // taking extra damage on head hit
    public int health = 100;
    private float shootingCooldown = 3f; // 2 seconds cooldown after shooting
    private float lastShootTime;
    private float groundCheckRadius = 10f;
    #endregion

    #region State Variables
    private bool isFacingRight = true;
    private bool isShooting = false;
    #endregion

    #region Components
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D bodyHurtBox;
    private CircleCollider2D headHurtBox;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;
    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyHurtBox = GetComponent<CapsuleCollider2D>();
        headHurtBox = GetComponent<CircleCollider2D>();
    }

    private void FixedUpdate()
    {
        animator.SetFloat("horizontalVelocity", Mathf.Abs(rb.velocity.x));
        DetectPlayerAndAct();
    }

    private void DetectPlayerAndAct()
    {
        // Example code for detecting the player on the same height level using a raycast
        RaycastHit2D hit = Physics2D.Raycast(muzzle.position, isFacingRight ? Vector2.right : Vector2.left, detectRange);
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            if (!isShooting && (Time.time > (lastShootTime + shootingCooldown)))
            { StartCoroutine(TelegraphAndShoot()); }
        }
        else
        {
            Walk();
        }
    }


    private void Walk()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int platformLayer = LayerMask.NameToLayer("OneWayPlatform");
        int walkableLayers = (1 << groundLayer) | (1 << platformLayer);

        float scaledOffset = groundCheckRadius * transform.localScale.x;  // Adjust the offset based on scale
        float scaledRayLength = groundCheckRadius * transform.localScale.y;  // Adjust ray length based on scale

        Vector2 groundCheck = new Vector2(isFacingRight ? transform.position.x + scaledOffset : transform.position.x - scaledOffset, transform.position.y - scaledOffset);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck, Vector2.down, scaledRayLength, walkableLayers);
        if (groundHit.collider == null)
        {
            Flip();
        }

        rb.velocity = new Vector2(isFacingRight ? walkingSpeed : -walkingSpeed, rb.velocity.y);
    }


    private IEnumerator TelegraphAndShoot()
    {
        isShooting = true;
        animator.SetBool("isShooting", isShooting);

        animator.SetTrigger("telegraph");
        yield return new WaitForSeconds(telegraphDuration);
        animator.SetTrigger("shoot");
        yield return new WaitForSeconds(shootAnimationDuration);

        lastShootTime = Time.time; // Record the time when the enemy shot
        // Spawn projectile etc...
        Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

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

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(FlashOnHit(Color.white));  // white or pink when taking non-lethal damage
        }
    }

    private IEnumerator FlashOnHit(Color color)
    {
        spriteRenderer.color = color;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        animator.SetTrigger("death");
        StartCoroutine(FlashOnHit(Color.red));  // red on lethal damage
        // disable any further behavior
        this.enabled = false;
        rb.velocity = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        // Draw floor detection raycast
        Gizmos.color = Color.yellow;
        float scaledOffset = groundCheckRadius * transform.localScale.x;  // Adjust the offset based on scale
        float scaledRayLength = groundCheckRadius * transform.localScale.y;  // Adjust ray length based on scale
                                                                // int groundLayer = LayerMask.NameToLayer("Ground");
                                                                // int platformLayer = LayerMask.NameToLayer("OneWayPlatform");
        Vector2 groundCheck = new Vector2(isFacingRight ? transform.position.x + scaledOffset : transform.position.x - scaledOffset, transform.position.y - scaledOffset);
        Gizmos.DrawLine(groundCheck, groundCheck + Vector2.down * scaledRayLength);

        // Draw player detection raycast
        Gizmos.color = Color.green;
        Vector2 rayStart = muzzle.position;
        Vector2 rayDirection = isFacingRight ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(rayStart, rayStart + rayDirection * detectRange);


        // Draw body hurtbox
        // This isn't working properly but whateverh
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

