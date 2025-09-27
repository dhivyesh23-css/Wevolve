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
    public TailController tailController;
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Underwater Walking")]
    public float moveSpeed = 3f;
    public float flapForce = 12f;
    public float airRotationSpeed = 300f;
    public float walkGravity = 0.5f;

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentState == MovementState.Walking)
        {
            SwitchState(MovementState.Penetrating);
        }

        if (currentState == MovementState.Penetrating)
        {
            if (Input.GetKey(KeyCode.F)) { tailController.StartRetraction(); }
            else if (Input.GetKeyUp(KeyCode.F)) { tailController.StopRetraction(); }
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
        else if (currentState == MovementState.Penetrating)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            anim.Play("Idle");
            tailController.StartPenetration();
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