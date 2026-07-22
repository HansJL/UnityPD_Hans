using UnityEngine;
using System;

public class CollisionPD : MonoBehaviour
{
    private LibPdInstance pdInstance;

    [Header("Midi Input Script Object")] 
    [SerializeField] private MidiInput midiInput;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float restY;
    private bool jumpRequested;

    private Action<int, int, int> onControlChangeCallback;
    private Action<int, int, int> onNoteOnCallback;
   
    [Header("Jump")] 
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private bool jumpWithSpace = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
        onControlChangeCallback = OnControlChangeCallback;
        onNoteOnCallback = OnNoteOnCallback;
    }
    void OnEnable()
    {
        midiInput.onControlChange += onControlChangeCallback;
        midiInput.onNoteOn += onNoteOnCallback;
    }
    void OnDisable()
    {
        midiInput.onControlChange -= onControlChangeCallback;
        midiInput.onNoteOn -= onNoteOnCallback;
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (rb.linearVelocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Prüft, ob wir das Sphere-Objekt treffen
        if (collision.gameObject.CompareTag("Sphere"))
        {

            if (pdInstance != null)
            {
                // Wir senden einen Float-Impuls an den Namen "trigger_cc71"
                pdInstance.SendFloat("collision", 127f);
                Debug.Log("[Unity] Impuls für CC71 an Pure Data geschickt.");
            }
        }
    }
     void OnControlChangeCallback(int channel, int control, int value)
    {
        // Pad touch: usually value > 0; release often sends 0
        if (channel == 1 && control == 71 && value > 0)
        {
            if (pdInstance != null)
            {
                Debug.Log("[Unity] Pad touch for CC71 detected.");
            }
        }
    }
    void OnNoteOnCallback(int channel, int note, int velocity)
    {
        if (channel == 9 && note == 40 && velocity > 0)
        {
            if (pdInstance != null)
            {
                pdInstance.SendFloat("sahOn", 127f);
                Debug.Log("[Unity] Sah on detected.");
            }
        }
    }
}
