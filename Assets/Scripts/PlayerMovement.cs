using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [Header("Components")]
    private Animator animator;
    // private CapsuleCollider2D capsuleCollider;
    private LayerMask playerLayerMask;
    // [SerializeField] private Transform playerSprite;
    private Rigidbody2D rigidBody;

    [Header("State Variables")]
    private bool facingRight = true;
    private bool crouching = false;
    public float horizontalMovement; // -1 | 0 | 1
    private bool jumpButtonDown;
    private int jumpSquatCounter;
    private Vector2 aimDirection;

    [Header("Player Attributes")]
    public float baseGravity;
    public float baseJumpForce = 3;
    [SerializeField] private int jumpSquatFrames = 4;
    public float shortHopForce = 1.0f;
    public float fullHopForce = 1.25f;
    public float movementSpeed = 4.0f;
    public float deadZone = 0.19f;
    public float tiltRadius = 0.6f; // This shouldn't do anything with arrow keys

    [Header("Grounded Check Variables")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private LayerMask oneWayPlatformLayer;
    [SerializeField] private LayerMask playerHurtBoxLayer;
    private int playerLayerNumber;
    private int playerHurtBoxLayerNumber;
    // private int groundLayerNumber;
    private int oneWayPlatformLayerNumber;
    public bool playerIsGrounded;

    [Header("Aim Rotation")]
    public Transform spriteTransform;
    public Transform rotationPivot;
    private Quaternion originalRotation;

    [Header("Shoot")]
    [SerializeField] private Transform muzzleAir;
    [SerializeField] private Transform muzzleGround;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int shotsPerBurst = 6;
    private int shotsFiredInBurst = 0;
    [SerializeField] private float timeBetweenShots = 0.17f;
    [SerializeField] private float timeBetweenBursts = 1f;
    private bool isFiring = false;
    private bool canFire = true;
    [SerializeField] private float bulletSpread = 2.5f;
    #endregion

    private void Start()
    {
        #region Get Components
        animator = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody2D>();
        #endregion

        #region Layer mask variable init
        playerLayerMask = 1 << gameObject.layer;
        playerLayerNumber = GetLayerFromMask(playerLayerMask);
        playerHurtBoxLayerNumber = GetLayerFromMask(playerHurtBoxLayer);
        // groundLayerNumber = GetLayerFromMask(groundLayerMask);
        oneWayPlatformLayerNumber = GetLayerFromMask(oneWayPlatformLayer);
        #endregion

        baseGravity = rigidBody.gravityScale;
        originalRotation = spriteTransform.rotation;
    }

    /* Poll for input every frame */
    private void Update()        // Poll for input
    {
        #region Crouch Input
        // Check for down on primary axis
        float verticalAxisInput = Input.GetAxisRaw("LeftVertical");
        bool crouchButtonDown = Input.GetButtonDown("Crouch");
        bool crouchButtonHeld = Input.GetButton("Crouch"); // Check if the "Crouch" button is being held down

        if ((verticalAxisInput < (-1 * tiltRadius) || crouchButtonHeld) && playerIsGrounded) // If either "down" is being held or "Crouch" is held and player is on the ground
        {
            Crouch();
        }
        else if (verticalAxisInput >= (-1 * tiltRadius) && !crouchButtonDown) // If neither "down" is being held nor "Crouch"
        {
            UnCrouch();
        }

        // Fast fall code remains unchanged
        if (verticalAxisInput < (-1 * tiltRadius) && !playerIsGrounded && rigidBody.velocity.y <= 0)
        {
            FastFall();
        }
        #endregion

        #region Horizontal Movement Input
        if (!crouching) { horizontalMovement = Input.GetAxisRaw("LeftHorizontal"); }
        #endregion

        #region Jump Input
        if (Input.GetButtonDown("Jump") && playerIsGrounded && jumpSquatCounter == 0)
        {
            jumpSquatCounter = jumpSquatFrames;
            jumpButtonDown = true;
        } // Jump();

        // Detect a shorthop if button is released before end of jumpSquat
        if (jumpButtonDown && Input.GetButtonUp("Jump") && jumpSquatCounter > 0)
        {
            jumpButtonDown = false;
        }
        #endregion

        #region Aim Input
        aimDirection = GetAimVector();
        #endregion

        #region Shoot
        if (Input.GetButtonDown("Fire1") && !crouching)
        {
            {
                Shoot();
            }
        }
        #endregion
    }

    /* Apply input at fixed interval */
    private void FixedUpdate()
    {
        CheckGrounded();
        HandleOneWayPlatformCollision();

        #region Horizontal Movement
        if (!crouching) rigidBody.velocity = new Vector2(horizontalMovement * movementSpeed, rigidBody.velocity.y);
        CheckFlipHorizontal();
        // animator.SetFloat("playerSpeed", Mathf.Abs(horizontalMovement));
        animator.SetFloat("playerSpeed", Mathf.Abs(rigidBody.velocity.x));
        #endregion

        #region Jump
        if (jumpSquatCounter > 0) // Stop counting down at 0
        {
            animator.SetBool("isCrouching", jumpSquatCounter > 0); // Using crouch sprite for jumpSquat anim

            jumpSquatCounter--;

            if (jumpSquatCounter == 0)
            {
                Jump();
            }
        }
        #endregion

        #region Air Aiming
        if (!playerIsGrounded && aimDirection.magnitude > deadZone)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            if (!facingRight)
                angle += 180; // Add an offset when facing left
            animator.transform.RotateAround(rotationPivot.position, Vector3.forward, angle - spriteTransform.rotation.eulerAngles.z);
        }
        else if (playerIsGrounded)
        {
            animator.transform.rotation = originalRotation;
        }
        #endregion
    }

    private void CheckFlipHorizontal() // Turn the player around
    {
        if (!playerIsGrounded) return;

        if (((horizontalMovement < 0) && facingRight) || ((horizontalMovement > 0) && !facingRight))
        {
            facingRight = !facingRight;
            Vector3 playerScale = transform.localScale;
            playerScale.x *= -1;
            transform.localScale = playerScale;

            // Flip muzzle orientations
            muzzleAir.localEulerAngles = new Vector3(muzzleAir.localEulerAngles.x, muzzleAir.localEulerAngles.y + 180, muzzleAir.localEulerAngles.z);
            muzzleGround.localEulerAngles = new Vector3(muzzleGround.localEulerAngles.x, muzzleGround.localEulerAngles.y + 180, muzzleGround.localEulerAngles.z);
        }
    }


    private void CheckGrounded()
    {
        bool groundedOnGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        bool groundedOnPlatform = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, oneWayPlatformLayer);

        playerIsGrounded = (groundedOnGround || groundedOnPlatform);

        if (playerIsGrounded)
        {
            rigidBody.gravityScale = baseGravity;
        }
        animator.SetBool("isGrounded", playerIsGrounded);
    }

    private void Crouch()
    {
        if (crouching) return; // don't want to run the rest of this code if we're already crouching
        // Debug.Log("Crouch");
        crouching = true;
        animator.SetBool("isCrouching", true);
    }

    private void FastFall()
    {
        if (!playerIsGrounded)
        {
            rigidBody.gravityScale = baseGravity * 2;
        }
    }

    private Vector2 GetAimVector()
    {
        float rightStickHorizontal = Input.GetAxis("RightHorizontal");
        float rightStickVertical = Input.GetAxis("RightVertical");
        Vector2 aimDir = new Vector2(rightStickHorizontal, rightStickVertical);
        if (aimDir.magnitude > deadZone)
        {
            aimDir.Normalize();
        }
        else // We honestly don't need to be normalizing this, idk it just seems correct
        {
            aimDir = Vector2.zero;
        }
        return aimDir;
    }
    private int GetLayerFromMask(LayerMask mask)
    {
        int layerNumber = 0;
        int maskValue = mask.value;
        while (maskValue > 0)
        {
            maskValue = maskValue >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }

    private void HandleOneWayPlatformCollision()
    {
        // Always ignore collision between capsule collider and one-way platforms
        Physics2D.IgnoreLayerCollision(playerHurtBoxLayerNumber, oneWayPlatformLayerNumber, true);

        if (rigidBody.velocity.y <= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayerNumber, oneWayPlatformLayerNumber, false);
        }
        else
        {
            Physics2D.IgnoreLayerCollision(playerLayerNumber, oneWayPlatformLayerNumber, true);
        }
    }

    private void Jump()
    {
        // Debug.Log("crouching: " + crouching);

        float jumpMultiplier = jumpButtonDown ? fullHopForce : shortHopForce; // Jump higher if the jump button is still held.
        if (crouching) jumpMultiplier *= 1.5f;
        float jumpForce = baseJumpForce * jumpMultiplier;

        rigidBody.velocity = new Vector2(rigidBody.velocity.x, jumpForce);
        if (crouching) UnCrouch();
        jumpButtonDown = false;
        animator.SetBool("isCrouching", false);
    }

    private void Shoot()
    {
        if (isFiring || !canFire) return;
        StartCoroutine(BurstFire());
    }

    private IEnumerator BurstFire()
    {
        isFiring = true;
        animator.SetBool("isFiring", true);

        while (shotsFiredInBurst < shotsPerBurst)
        {
            float randomSpread = Random.Range(-bulletSpread, bulletSpread);

            if (playerIsGrounded)
            {
                Quaternion spreadRotation = Quaternion.Euler(0, 0, randomSpread);
                Instantiate(projectilePrefab, muzzleGround.position, muzzleGround.rotation * spreadRotation);
            }
            else
            {
                Quaternion spreadRotation = Quaternion.Euler(0, 0, randomSpread * 2); // Less accurate in the air
                Instantiate(projectilePrefab, muzzleAir.position, muzzleAir.rotation * spreadRotation);
            }

            shotsFiredInBurst++;
            yield return new WaitForSeconds(timeBetweenShots);
        }
        animator.SetBool("isFiring", false);

        shotsFiredInBurst = 0;
        canFire = false;
        yield return new WaitForSeconds(timeBetweenBursts);
        canFire = true;
        isFiring = false;
        
    }


    private void UnCrouch()
    { // We might integrate this with the animator so it gets its own function.
        if (!crouching) return;
        crouching = false;
        animator.SetBool("isCrouching", false);
    }

    private void OnDrawGizmos() // For debugging
    {
        Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);
        DrawArrow(muzzleGround.position, muzzleGround.right * 0.5f);  // Assuming "right" is the forward direction for shooting
        DrawArrow(muzzleAir.position, muzzleAir.right * 0.5f);
    }

    private void DrawArrow(Vector3 startPos, Vector3 direction)
    {
        float arrowHeadAngle = 25.0f;
        float arrowHeadLength = 0.2f;

        Vector3 rightArrow = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, arrowHeadLength);
        Vector3 leftArrow = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, arrowHeadLength);

        Gizmos.DrawRay(startPos, direction);          // Arrow shaft
        Gizmos.DrawRay(startPos + direction, rightArrow);  // Right side of arrow head
        Gizmos.DrawRay(startPos + direction, leftArrow);   // Left side of arrow head
    }
}
