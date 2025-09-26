using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BacteriophageController : MonoBehaviour
{
    // === STATE ===
    public enum MovementState { Walking, Swimming, Penetrating }
    public MovementState currentState = MovementState.Walking;
    private bool isGrounded;

    // === COMPONENTS & REFERENCES ===
    public Transform[] upperLegs;
    public Transform[] lowerLegs;
    public TailController tailController;
    private Rigidbody2D rb;
    private Animator anim;

    // (All of your public variables remain the same)
    [Header("Underwater Walking")]
    public float moveSpeed = 3f;
    public float flapForce = 12f;
    public float airRotationSpeed = 300f;
    public float walkGravity = 0.5f;
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask whatIsGround;
    [Header("Swimming (Reset Mode)")]
    public float swimForce = 3f;
    public float swimGravity = 0.3f;
    public float swimDrag = 2f;
    public float maxTiltAngle = 20f;
    public float tiltSpeed = 5f;
    public float swimModeDuration = 1f;
    [Header("Animation")]
    public float swimAnimationSpeed = 2f;
    public float swimAmplitude = 25f;

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

        // --- MODIFIED: Handle HOLDING F for retraction ---
        if (currentState == MovementState.Walking)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SwitchState(MovementState.Swimming);
                StartCoroutine(SwimModeTimer());
            }
        }
        else if (currentState == MovementState.Penetrating)
        {
            // When F is held down, start retracting
            if (Input.GetKey(KeyCode.F))
            {
                tailController.StartRetraction();
            }
            // When F is released, stop retracting
            else if (Input.GetKeyUp(KeyCode.F))
            {
                tailController.StopRetraction();
            }
        }
        
        HandleJumpInput();
        HandleAnimation();
    }

    IEnumerator SwimModeTimer()
    {
        yield return new WaitForSeconds(swimModeDuration);
        if (currentState == MovementState.Swimming) { SwitchState(MovementState.Walking); }
    }

    void FixedUpdate()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        HandleMovement();
    }

    void HandleJumpInput()
    {
        if (currentState == MovementState.Penetrating) return;
        if (currentState == MovementState.Walking)
        {
            if (Input.GetButtonDown("Jump"))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(transform.up * flapForce, ForceMode2D.Impulse);
                anim.SetTrigger("Jump");
            }
        }
    }

    void HandleMovement()
    {
        if (currentState == MovementState.Penetrating)
        {
            // MODIFIED: Only allow steering if we are NOT currently retracting
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
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
                rb.angularVelocity = 0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * tiltSpeed);
            }
            else { rb.AddTorque(-horizontalInput * airRotationSpeed * Time.fixedDeltaTime); }

            if (horizontalInput > 0.1f) { transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); }
            else if (horizontalInput < -0.1f) { transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); }
        }
        else if (currentState == MovementState.Swimming)
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
            anim.SetBool("isWalking", isMoving && isGrounded);
            anim.SetBool("isGrounded", isGrounded);
        }
        else if (currentState == MovementState.Swimming)
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
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = walkGravity;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 1.0f;
            rb.angularVelocity = 0f;
            transform.rotation = Quaternion.identity;
        }
        else if (currentState == MovementState.Swimming)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = swimGravity;
            rb.linearDamping = swimDrag;
            rb.angularDamping = 0.05f;
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
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}