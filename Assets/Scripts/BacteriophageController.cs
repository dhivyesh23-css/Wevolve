using UnityEngine;
using System.Collections; // Required for using coroutines (IEnumerator)

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BacteriophageController : MonoBehaviour
{
    // === STATE ===
    public enum MovementState { Walking, Swimming }
    public MovementState currentState = MovementState.Walking;
    private bool isGrounded;

    // === COMPONENTS & REFERENCES ===
    public Transform[] upperLegs;
    public Transform[] lowerLegs;
    private Rigidbody2D rb;
    private Animator anim;

    // === PHYSICS & MOVEMENT SETTINGS ===
    [Header("Underwater Walking")]
    public float moveSpeed = 3f; // Reduced for underwater feel
    public float flapForce = 12f;
    public float airRotationSpeed = 300f;
    public float walkGravity = 0.5f; // Reduced for underwater feel

    [Header("Ground Check")]
    public Transform groundCheck; // Assign a child object at the phage's feet
    public float groundCheckRadius = 0.3f;
    public LayerMask whatIsGround; // Set this in the inspector to your ground layer

    [Header("Swimming (Reset Mode)")]
    public float swimForce = 3f;
    public float swimGravity = 0.3f;
    public float swimDrag = 2f;
    public float maxTiltAngle = 20f;
    public float tiltSpeed = 5f;
    public float swimModeDuration = 1f;

    // === ANIMATION SETTINGS ===
    [Header("Animation")]
    public float swimAnimationSpeed = 2f;
    public float swimAmplitude = 25f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        SwitchState(MovementState.Walking);

        if (groundCheck == null)
        {
            Debug.LogError("Ground Check object is not assigned. Please create a child object at the character's feet and assign it in the Inspector.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && currentState == MovementState.Walking)
        {
            SwitchState(MovementState.Swimming);
            StartCoroutine(SwimModeTimer());
        }

        HandleJumpInput();
        HandleAnimation();
    }

    IEnumerator SwimModeTimer()
    {
        yield return new WaitForSeconds(swimModeDuration);
        if (currentState == MovementState.Swimming)
        {
            SwitchState(MovementState.Walking);
        }
    }

    void FixedUpdate()
    {
        // --- FIX: Add a null check to prevent the error if groundCheck is not assigned ---
        if (groundCheck == null)
        {
            // We already log an error in Start, so no need to spam the console here.
            return; // Exit the method early to prevent the error
        }

        // Perform ground check using a circle cast
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        
        HandleMovement();
    }

    void HandleJumpInput()
    {
        if (currentState == MovementState.Walking)
        {
            // Spacebar press
            if (Input.GetButtonDown("Jump"))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                // Applies force in the direction the top of the object is pointing
                rb.AddForce(transform.up * flapForce, ForceMode2D.Impulse);
                anim.SetTrigger("Jump");
            }
        }
    }


    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if (currentState == MovementState.Walking)
        {
            if (isGrounded)
            {
                // --- ON GROUND: A/D moves character left and right ---
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

                // When grounded, stop rotation and smoothly reset to be upright
                rb.angularVelocity = 0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * tiltSpeed);
            }
            else
            {
                // --- IN AIR: A/D rotates the character ---
                rb.AddTorque(-horizontalInput * airRotationSpeed * Time.fixedDeltaTime);
            }

            // Handle flipping character sprite based on movement direction
            if (horizontalInput > 0.1f)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (horizontalInput < -0.1f)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
        else // Swimming
        {
            float verticalInput = Input.GetAxis("Vertical");
            Vector2 swimDirection = new Vector2(horizontalInput, verticalInput).normalized;
            rb.AddForce(swimDirection * swimForce);

            float targetZ = (rb.linearVelocity.y * 0.5f) - (horizontalInput * maxTiltAngle * maxTiltAngle * 0.5f);
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * tiltSpeed);
        }
    }

    void HandleAnimation()
    {
        if (currentState == MovementState.Walking)
        {
            bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;
            anim.SetBool("isWalking", isMoving && isGrounded); // Only show walk anim if grounded
            anim.SetBool("isGrounded", isGrounded);

            // --- DEBUGGING LOG ---
            // Check your console window for these messages when you play the game.
            // This will tell you if the ground check is working.
            Debug.Log("isGrounded: " + isGrounded + " | isMoving: " + isMoving);
        }
        else // Swimming (still uses procedural animation)
        {
            float swimTimer = Time.time * swimAnimationSpeed;
            float direction = Mathf.Sign(transform.localScale.x);
            for (int i = 0; i < upperLegs.Length; i++)
            {
                float phase = i * 0.7f;
                float angle = Mathf.Sin(swimTimer + phase) * swimAmplitude * direction;
                upperLegs[i].localRotation = Quaternion.Euler(0, 0, angle);
                lowerLegs[i].localRotation = Quaternion.Euler(0, 0, angle * 0.75f);
            }
        }
    }

    void SwitchState(MovementState newState)
    {
        currentState = newState;
        anim.enabled = (currentState == MovementState.Walking);

        if (currentState == MovementState.Walking)
        {
            // --- MODIFIED: Physics for underwater feel ---
            rb.gravityScale = walkGravity;
            rb.linearDamping = 1.5f; // Increased drag
            rb.angularDamping = 1.0f; // Add angular drag

            // When switching back, immediately stop rotation and reset angle
            rb.angularVelocity = 0f;
            transform.rotation = Quaternion.identity;
        }
        else // Swimming
        {
            rb.gravityScale = swimGravity;
            rb.linearDamping = swimDrag;
            rb.angularDamping = 0.05f; // Default angular drag
        }
    }

    // --- NEW DEBUGGING METHOD ---
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        
        // This draws a red wire sphere in your Scene view at the groundCheck position.
        // It helps you visualize if the ground check area is correctly positioned.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}

