using System;
using UnityEngine;

/// <summary>
/// 2D patrol walker driven by PdMicBridge (mic pitch/envelope speed + clap jump).
/// No MIDI. Pair with PdMicBridge on the same GameObject (or elsewhere in the scene).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterWalker2D : MonoBehaviour
{
    public event Action OnSphereCollision;

    [Header("Pure Data (sphere hit bang)")]
    [SerializeField] private LibPdInstance pdInstance;
    [SerializeField] private string sphereHitReceiver = "sahOn";

    [Header("Speed")]
    [Range(0.1f, 20f)]
    public float globalSpeedMultiplier = 1f;
    [SerializeField] private float minSpeed = 0.2f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float speedChangeSmoothness = 3f;
    [SerializeField] private bool useMicSpeed = true;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 20f;
    public float leftBoundary = -11.5f;
    public float rightBoundary = 11.5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private bool jumpWithSpace = true;

    [Header("Animation")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float restY;
    private float targetSpeedMultiplier = 1f;
    private bool jumpRequested;
    private int direction = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        PdMicBridge.OnSpeedChanged += SetGlobalSpeed;
        PdMicBridge.OnJumpBang += OnPdJump;
    }

    private void OnDisable()
    {
        PdMicBridge.OnSpeedChanged -= SetGlobalSpeed;
        PdMicBridge.OnJumpBang -= OnPdJump;
    }

    private void Start()
    {
        restY = transform.position.y;
        targetSpeedMultiplier = globalSpeedMultiplier;

        if (pdInstance == null)
            pdInstance = GetComponentInChildren<LibPdInstance>()
                ?? FindFirstObjectByType<LibPdInstance>();
    }

    private void Update()
    {
        globalSpeedMultiplier = Mathf.MoveTowards(
            globalSpeedMultiplier,
            targetSpeedMultiplier,
            Time.deltaTime * speedChangeSmoothness
        );

        if (animator != null)
            animator.speed = baseAnimationSpeed * globalSpeedMultiplier;

        if (jumpWithSpace && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
            TriggerJump();

        if (transform.position.x >= rightBoundary && direction == 1)
            FlipDirection(-1);
        else if (transform.position.x <= leftBoundary && direction == -1)
            FlipDirection(1);
    }

    private void FixedUpdate()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        float currentSpeed = baseMoveSpeed * globalSpeedMultiplier;
        rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            jumpRequested = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (rb.linearVelocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    public void TriggerJump()
    {
        jumpRequested = true;
    }

    public void SetGlobalSpeed(float multiplier)
    {
        if (!useMicSpeed)
            return;

        targetSpeedMultiplier = Mathf.Clamp(multiplier, minSpeed, maxSpeed);
    }

    private void OnPdJump()
    {
        TriggerJump();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleSphereHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleSphereHit(other.gameObject);
    }

    private void HandleSphereHit(GameObject other)
    {
        if (!other.CompareTag("Sphere"))
            return;

        FlipDirection(direction * -1);
        OnSphereCollision?.Invoke();

        if (pdInstance != null)
            pdInstance.SendBang(sphereHitReceiver);
    }

    private void FlipDirection(int newDirection)
    {
        direction = newDirection;
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;
    }
}
