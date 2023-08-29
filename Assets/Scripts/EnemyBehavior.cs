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
        string stateName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        Debug.Log("Animator - Current State: " + stateName);


        animator.SetFloat("horizontalVelocity", Mathf.Abs(rb.velocity.x));
        DetectPlayerAndAct();
    }

    private void DetectPlayerAndAct()
    {
        RaycastHit2D hit = Physics2D.Raycast(muzzle.position, isFacingRight ? Vector2.right : Vector2.left, detectRange);

        if (hit.collider != null && hit.collider.gameObject.layer == playerLayer)
        {
            Debug.Log("Player detected - EXTERMINATE");
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
        isShooting = true;
        animator.SetBool("isShooting", isShooting);
        animator.SetTrigger("telegraph");

        rb.velocity = new Vector2(0, rb.velocity.y);  // Stop horizontal movement

        yield return new WaitForSeconds(telegraphDuration);

        animator.SetTrigger("shoot");
        Instantiate(enemyProjectilePrefab, muzzle.position, muzzle.rotation);

        lastShootTime = Time.time; // Record the time when the enemy shot
                                   // Spawn projectile etc...
        Debug.Log("Kaboom");

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


    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        health -= damage;
        if (health <= 0)
        {
            // Not so sure about this...
            if ((hitDirection.x > 0 && isFacingRight) || (hitDirection.x < 0 && !isFacingRight))
            {
                // The enemy was hit from the front
                animator.SetTrigger("fallBackward");
            }
            else
            {
                // The enemy was hit from behind
                animator.SetTrigger("fallForward");
            }

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

    public void HandleDeath(Vector2 hitDirection)
    {
        if (Vector2.Dot(hitDirection, transform.right) > 0)
        {
            // The hit came from the front
            animator.SetTrigger("fallBackward");
        }
        else
        {
            // The hit came from behind
            animator.SetTrigger("fallForward");
        }

        StartCoroutine(FlashOnHit(Color.red));  // red on lethal damage
                                                // disable any further behavior
        this.enabled = false;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
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

