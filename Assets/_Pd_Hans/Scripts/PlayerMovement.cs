using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Global Control")]
    [Tooltip("Kann im Inspector geschoben ODER von Pd gesteuert werden.")]
    [Range(0.1f, 3f)] // Erzeugt einen coolen Schieberegler im Inspector von 0.1 bis 3-facher Geschwindigkeit
    public float globalSpeedMultiplier = 1f; 

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;  
    [SerializeField] private float leftBoundary = -5f;  
    [SerializeField] private float rightBoundary = 5f;  

    [Header("Animation Settings")]
    [SerializeField] private float baseAnimationSpeed = 1f; 

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private int direction = 1; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Live-Anpassung der Animation
        if (animator != null)
        {
            animator.speed = baseAnimationSpeed * globalSpeedMultiplier;
        }

        // Grenzen prüfen
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
        // Bewegung anwenden
        float currentSpeed = baseMoveSpeed * globalSpeedMultiplier;
        Vector2 movement = new Vector2(direction * currentSpeed * Time.fixedDeltaTime, 0f);
        rb.position += movement;
    }

    // DIESE FUNKTION WIRD VON DEINEM PD-EMPFÄNGER-SKRIPT AUFGERUFEN
    public void SetGlobalSpeedFromMidi(int midiValue)
    {
        // Rechnet 0-127 in einen Bereich von 0.1 bis 2.5 um
        globalSpeedMultiplier = 0.1f + ((float)midiValue / 127f) * 2.4f;
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