using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BacteriophageController : MonoBehaviour
{
    // === STATE ===
    public enum MovementState { Walking, Penetrating }
    public MovementState currentState = MovementState.Walking;
    private bool isGrounded;
    private float jumpBufferTimer;

    // === COMPONENTS & REFERENCES ===
    public Transform[] upperLegs;
    public Transform[] lowerLegs;
    // NOTE: TailController class methods (like IsRetracting) are assumed to exist.
    public TailController tailController; 
    private Rigidbody2D rb;
    private Animator anim;
    
    // State flag to track tail retraction for the F key toggle
    private bool isTailRetracting = false; 

    [Header("Underwater Walking")]
    public float moveSpeed = 3f;
    public float flapForce = 12f;
    public float airRotationSpeed = 300f;
    public float walkGravity = 0.5f;

    [Header("Antibody Interaction")]
    [Tooltip("The Tag used for Antibody GameObjects. MUST be defined in Unity's Tag Manager.")]
    public string antibodyTag = "Antibody";
    [Tooltip("The speed will be multiplied by this factor when touching an Antibody.")]
    public float antibodySlowFactor = 0.3f; // e.g., 30% of original speed
    private float originalMoveSpeed;
    private int antibodyContactCount = 0;
    // Removed unused variable 'isSlowedByAntibody'

    [Header("Surface Walking")]
    [Tooltip("How strongly the phage sticks to curved surfaces.")]
    public float surfaceStickForce = 50f;
    [Tooltip("How fast the phage rotates to align with the ground.")]
    public float surfaceAlignSpeed = 15f;
    private bool isStickingToSurface = false;
    private Vector2 groundNormal;
    [Tooltip("The duration during which the phage cannot stick to surfaces after a jump.")]
    public float jumpBufferDuration = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask whatIsGround;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        SwitchState(MovementState.Walking);
        if (groundCheck == null) { Debug.LogError("Ground Check object is not assigned."); }
        if (tailController == null) { Debug.LogError("Tail Controller is not assigned in the Inspector!"); }
        
        // Store the original speed for recovery after slowdown
        originalMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentState == MovementState.Walking)
        {
            SwitchState(MovementState.Penetrating);
        }

        if (currentState == MovementState.Penetrating)
        {
            // Tail Retraction Toggle
            if (Input.GetKeyDown(KeyCode.F)) 
            {
                if (isTailRetracting) 
                { 
                    tailController.StopRetraction();
                    isTailRetracting = false;
                } 
                else 
                {
                    tailController.StartRetraction();
                    isTailRetracting = true;
                }
            }
        }

        HandleJumpInput();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        if (groundCheck == null) return;

        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
        }

        CheckSurface();
        HandleMovement();
    }

    // --- Collision handling for Antibody Slowdown (using Trigger) ---

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other object's tag matches the antibody tag.
        // We use 'tag == antibodyTag' for robustness against the Tag being undefined in the Editor.
        if (other.gameObject.tag == antibodyTag)
        {
            antibodyContactCount++;
            
            // Only apply slow effect on the first contact
            if (antibodyContactCount == 1)
            {
                // Reduce the current move speed
                moveSpeed = originalMoveSpeed * antibodySlowFactor; 
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if the other object's tag matches the antibody tag.
        if (other.gameObject.tag == antibodyTag)
        {
            antibodyContactCount--;
            
            // Only restore speed if all antibody contacts have ended
            if (antibodyContactCount <= 0)
            {
                antibodyContactCount = 0; // Safety clamp
                // Restore the original move speed
                moveSpeed = originalMoveSpeed;
            }
        }
    }
    // --- END Collision handling ---

    void CheckSurface()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, -transform.up, groundCheckRadius + 0.2f, whatIsGround);

        if (hit.collider != null && currentState == MovementState.Walking && jumpBufferTimer <= 0)
        {
            isGrounded = true;
            isStickingToSurface = true;
            groundNormal = hit.normal;

            rb.gravityScale = 0;
            rb.AddForce(-groundNormal * surfaceStickForce);

            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, surfaceAlignSpeed * Time.fixedDeltaTime);
        }
        else
        {
            isGrounded = false;
            if (isStickingToSurface)
            {
                isStickingToSurface = false;
            }

            if (currentState == MovementState.Walking)
            {
                // This will use the current 'moveSpeed' which is either original or slowed
                rb.gravityScale = walkGravity; 
            }
        }
    }

    void HandleJumpInput()
    {
        if (currentState != MovementState.Walking) return;

        // Ground jump
        if (Input.GetButtonDown("Jump") && isStickingToSurface)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(groundNormal * flapForce, ForceMode2D.Impulse);
            anim.SetTrigger("Jump");
            jumpBufferTimer = jumpBufferDuration;
        }
        // Air jump
        else if (Input.GetButtonDown("Jump") && !isStickingToSurface)
        {
            rb.AddForce(transform.up * flapForce, ForceMode2D.Impulse);
            anim.SetTrigger("Jump");
        }
    }

    void HandleMovement()
    {
        if (currentState == MovementState.Penetrating)
        {
            // Assuming TailController.IsRetracting() exists for rotation check
            if (!tailController.IsRetracting())
            {
                float tailRotationInput = Input.GetAxisRaw("Horizontal");
                tailController.UpdateTailDirection(tailRotationInput);
            }
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");

        if (currentState == MovementState.Walking)
        {
            if (isStickingToSurface)
            {
                // Uses the current, potentially slowed, moveSpeed
                rb.linearVelocity = transform.right * horizontalInput * moveSpeed; 
            }
            else
            {
                rb.AddTorque(-horizontalInput * airRotationSpeed * Time.fixedDeltaTime);
            }

            if (horizontalInput > 0.1f) { transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); }
            else if (horizontalInput < -0.1f) { transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); }
        }
    }

    void HandleAnimation()
    {
        if (currentState == MovementState.Walking)
        {
            bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;
            anim.SetBool("isWalking", isMoving && isGrounded);
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    void SwitchState(MovementState newState)
    {
        if (newState == MovementState.Walking)
        {
            isStickingToSurface = false;
        }

        currentState = newState;
        anim.enabled = (currentState == MovementState.Walking);
        
        if (currentState == MovementState.Walking)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = walkGravity;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 1.0f;
            rb.angularVelocity = 0f;
        }
        else if (newState == MovementState.Penetrating)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            anim.Play("Idle");
            tailController.StartPenetration();
            isTailRetracting = false; // Reset the toggle state
        }
    }

    public void EndPenetrationMode() { SwitchState(MovementState.Walking); }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position - transform.up * (groundCheckRadius + 0.2f));
    }
}
