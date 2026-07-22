using UnityEngine;

public class CollisionPD : MonoBehaviour
{
    private LibPdInstance pdInstance;

    [Header("Midi Input Script Object")] 
    [SerializeField] private MidiInput midiInput;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float restY;
    private bool jumpRequested;

    [Header("Collision Sound File")] 
    [SerializeField] private AudioClip collisionClip;
    [SerializeField] [Range(0f, 1f)] private float collisionVolume = 1f;

    [Header("Jump")] 
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private bool jumpWithSpace = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collisionClip != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        restY = transform.position.y;
        
        // Findet den PD Audio Manager in der Szene
        pdInstance = FindFirstObjectByType<LibPdInstance>();
        if (pdInstance == null)
        {
            Debug.LogError("LibPdInstance konnte in der Szene nicht gefunden werden!");
        }
    }

    void Update()
    {
        if (jumpWithSpace && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        if (jumpRequested)
        {
            jumpRequested = false;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        if (rb.velocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.velocity = new Vector2(rb.velocity.x, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Prüft, ob wir das Sphere-Objekt treffen
        if (collision.gameObject.CompareTag("Sphere"))
        {
            if (collisionClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(collisionClip, collisionVolume);
            }

            if (pdInstance != null)
            {
                // Wir senden einen Float-Impuls an den Namen "trigger_cc71"
                pdInstance.SendFloat("trigger_cc71", 127f);
                Debug.Log("[Unity] Impuls für CC71 an Pure Data geschickt.");
            }
        }
    }
}
