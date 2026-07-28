using UnityEngine;

public class PlayerMove_B : MonoBehaviour
{
    [Header("Global Control")]
    [Range(0.1f, 3f)]
    public float globalSpeedMultiplier = 1f;

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float leftBoundary = -5f;
    [SerializeField] private float rightBoundary = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float defaultJumpForce = 10f;
    [SerializeField] private bool allowSpaceJump = true;

    [Header("Animation Settings")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    [Header("Pure Data Settings")]
    [SerializeField] private string collisionPdReceiver = "sahOn";

    // Komponenten & Instanzen
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private LibPdInstance pdInstance;

    // Interne Statusvariablen
    private int direction = 1;
    private float restY;
    private bool jumpRequested;
    private float currentJumpForce;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (rb != null) 
        {
            rb.freezeRotation = true;
        }
    }

    private void Start()
    {
        restY = transform.position.y;
        currentJumpForce = defaultJumpForce;

        // PD Instanz direkt im Player suchen
        pdInstance = FindFirstObjectByType<LibPdInstance>();
        if (pdInstance == null)
        {
            Debug.LogWarning("[PlayerMove_B] LibPdInstance wurde in der Szene nicht gefunden.");
        }
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.speed = baseAnimationSpeed * globalSpeedMultiplier;
        }

        if (allowSpaceJump && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
        {
            PerformJump(defaultJumpForce);
        }

        if (transform.position.x >= rightBoundary && direction == 1)
        {
            direction = -1;
            if (spriteRenderer != null) spriteRenderer.flipX = true;
        }
        else if (transform.position.x <= leftBoundary && direction == -1)
        {
            direction = 1;
            if (spriteRenderer != null) spriteRenderer.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float currentSpeed = baseMoveSpeed * globalSpeedMultiplier;
        Vector2 movement = new Vector2(direction * currentSpeed * Time.fixedDeltaTime, 0f);
        rb.position += movement;

        if (jumpRequested)
        {
            jumpRequested = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
        }

        if (rb.linearVelocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    // --- PUBLIC METHODS (Für MidiMapping oder andere Skripte) ---

    public void PerformJump(float force)
    {
        currentJumpForce = force;
        jumpRequested = true;
    }

    public void PerformJump()
    {
        PerformJump(defaultJumpForce);
    }

    public void SetGlobalSpeed(float multiplier)
    {
        globalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
    }

    // --- KOLLISIONEN ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Sphere"))
        {
            // Sendet direkt an Pure Data, wenn eine Kollision stattfindet
            if (pdInstance != null && !string.IsNullOrEmpty(collisionPdReceiver))
            {
                pdInstance.SendBang(collisionPdReceiver);
                Debug.Log($"[PlayerMove_B] Kollisions-Bang an PD-Receiver '{collisionPdReceiver}' gesendet.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 leftTarget = new Vector3(leftBoundary, transform.position.y, transform.position.z);
        Vector3 rightTarget = new Vector3(rightBoundary, transform.position.y, transform.position.z);
        Gizmos.DrawLine(leftTarget + Vector3.up, leftTarget + Vector3.down);
        Gizmos.DrawLine(rightTarget + Vector3.up, rightTarget + Vector3.down);
        Gizmos.DrawLine(leftTarget, rightTarget);
    }
}