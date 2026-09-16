using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCollisionAndMovement : MonoBehaviour
{
    public event Action OnSphereCollision;

    [Header("Pure Data Reference")]
    public LibPdInstance pdInstance;
    public string receiverPdGo = "sahOn";

    [Header("Movement Settings")]
    public float baseMoveSpeed = 20f;
    public float leftBoundary = -11.5f;
    public float rightBoundary = 11.5f;
    public int direction = 1;

    [Header("Speed Multipliers (Public for external controllers)")]
    public float globalSpeedMultiplier = 1f;
    public float targetSpeedMultiplier = 1f;
    public float minSpeed = 0.2f;
    public float maxSpeed = 20.0f;
    public float speedChangeSmoothness = 3f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public bool jumpWithSpace = true;

    [Header("Animation")]
    public float baseAnimationSpeed = 1f;

    // Internes
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float restY;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        restY = transform.position.y;
        targetSpeedMultiplier = globalSpeedMultiplier;

        if (pdInstance == null)
            pdInstance = GetComponentInChildren<LibPdInstance>();
    }

    void Update()
    {
        // Tempo sanft anpassen
        globalSpeedMultiplier = Mathf.MoveTowards(
            globalSpeedMultiplier, 
            targetSpeedMultiplier, 
            Time.deltaTime * speedChangeSmoothness
        );

        if (animator != null)
            animator.speed = baseAnimationSpeed * globalSpeedMultiplier;

        if (jumpWithSpace && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
            TriggerJump();

        // Grenzen prüfen & Umkehren
        if (transform.position.x >= rightBoundary && direction == 1)
            FlipDirection(-1);
        else if (transform.position.x <= leftBoundary && direction == -1)
            FlipDirection(1);
    }

    void FixedUpdate()
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        float currentSpeed = baseMoveSpeed * globalSpeedMultiplier;
        Vector2 movement = new Vector2(direction * currentSpeed * Time.fixedDeltaTime, 0f);
        rb.position += movement;

        if (rb.linearVelocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    public void TriggerJump()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public void SetGlobalSpeed(float multiplier)
    {
        targetSpeedMultiplier = Mathf.Clamp(multiplier, minSpeed, maxSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Sphere"))
        {
            // 1. Richtung umkehren (einfach den negativen Wert der aktuellen Richtung übergeben)
            int newDir = -direction;
            FlipDirection(newDir);

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            
                // 2. Spieler minimal in die NEUE Laufrichtung verschieben, 
                // damit er aus dem Collider der Sphere herausgeschoben wird:
                rb.position += new Vector2(0.1f * direction, 0f);
            }

            OnSphereCollision?.Invoke();

            if (pdInstance != null)
                pdInstance.SendBang(receiverPdGo);
        }
    }

    public void FlipDirection(int newDirection)
    {
        direction = newDirection;

        // Dreht das Sprite visuell um:
        // Wenn direction = 1 (rechts) -> flipX = false
        // Wenn direction = -1 (links) -> flipX = true
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (direction < 0);
        }
    }
}