using UnityEngine;
using System;

public class CollisionEnvPD : MonoBehaviour
{
    public event Action OnSphereCollision;

    [Header("Pure Data")]
    [SerializeField] private LibPdInstance pdInstance;
    [SerializeField] private string ReceiverPdGo = "sahOn";

    [Header("Global Control")]
    [Range(0.1f, 20f)]
    public float globalSpeedMultiplier = 1f;

    [Tooltip("Geschwindigkeit der Annäherung an den Envelope-Wert (höher = schneller)")]
    [SerializeField] private float speedChangeSmoothness = 10f;

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;
    public float leftBoundary = -5f;
    public float rightBoundary = 5f;

    [Header("Animation Settings")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    [Header("MIDI Input Source")] 
    [SerializeField] private MidiInput midiInput;
    [SerializeField] private string ReceiverFromKey = "sahOnKey";

    [Header("Pad Settings (Jump / Bang)")]
    [SerializeField] private int padMidiChannel = 1;
    [SerializeField] private int targetPadNote = 36; 

    [Header("Keyboard Range (Tempo per Tasten)")]
    [SerializeField] private int keyMidiChannel = 1;
    [SerializeField] private int targetLowKeyNote = 48; 
    [SerializeField] private int targetHighKeyNote = 72;

    [Header("Knob Settings (Tempo per Drehregler)")]
    [SerializeField] private int knobMidiChannel = 1;
    [SerializeField] private int targetCcControl = 1;

    [Header("Speed Limits")]
    [Tooltip("Mindestgeschwindigkeit (Stille im Mikrofon)")]
    [SerializeField] private float minSpeed = 0.2f;
    [Tooltip("Maximalgeschwindigkeit (Voller Mikrofonausschlag)")]
    [SerializeField] private float maxSpeed = 3.0f;

    [Header("Live MIDI Display (Nur zum Ablesen)")]
    [SerializeField] private int lastReceivedChannel;
    [SerializeField] private int lastReceivedNote;
    [SerializeField] private int lastReceivedVelocity;
    [SerializeField] private int lastReceivedCC;
    [SerializeField] private int lastReceivedCCValue;

    [Header("Jump")] 
    [SerializeField] private float jumpForce = 10f;       
    [SerializeField] private bool jumpWithSpace = true;    

    // Komponenten & Internes
    private Rigidbody2D rb;               
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float restY;                  
    private bool jumpRequested;           
    private bool pdPadBangRequested;
    private int direction = 1;

    // Für fließenden Geschwindigkeitsübergang
    private float targetSpeedMultiplier = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        // ÄNDERUNG 1: Anbindung an den PdEnvReceiver
        PdEnvReceiver.OnEnvelopeChanged += SetGlobalSpeed;
        
        if (midiInput != null)
        {
            midiInput.onControlChange += OnControlChangeCallback;
            midiInput.onNoteOn += OnNoteOnCallback;
            Debug.Log($"[CollisionPD] MIDI-Events erfolgreich auf {gameObject.name} registriert.");
        }
        else
        {
            Debug.LogWarning($"[CollisionPD] {name}: MidiInput ist im Inspector NICHT zugewiesen!");
        }
    }

    void OnDisable()
    {
        // ÄNDERUNG 2: Abmeldung vom PdEnvReceiver
        PdEnvReceiver.OnEnvelopeChanged -= SetGlobalSpeed;
        
        if (midiInput != null)
        {
            midiInput.onControlChange -= OnControlChangeCallback;
            midiInput.onNoteOn -= OnNoteOnCallback;
        }
    }

    void Start()
    {
        restY = transform.position.y;
        targetSpeedMultiplier = globalSpeedMultiplier;

        if (pdInstance == null)
            pdInstance = GetComponentInChildren<LibPdInstance>();

        if (pdInstance == null)
            Debug.LogError($"[CollisionPD] {name}: LibPdInstance fehlt!");
    }

    void Update()
    {
        // ÄNDERUNG 3: Exponentielle Glättung (Mathf.Lerp) für dynamisches Envelope Following
        globalSpeedMultiplier = Mathf.Lerp(
            globalSpeedMultiplier, 
            targetSpeedMultiplier, 
            Time.deltaTime * speedChangeSmoothness
        );

        if (animator != null)
        {
            animator.speed = baseAnimationSpeed * globalSpeedMultiplier;
        }

        if (jumpWithSpace && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
        {
            jumpRequested = true;
        }

        if (pdPadBangRequested)
        {
            pdPadBangRequested = false;
            if (pdInstance != null)
            {
                pdInstance.SendBang(ReceiverFromKey);
                Debug.Log("[Unity -> PD] Bang für Pad gedrückt gesendet.");
            }
        }

        if (transform.position.x >= rightBoundary && direction == 1)
        {
            FlipDirection(-1);
        }
        else if (transform.position.x <= leftBoundary && direction == -1)
        {
            FlipDirection(1);
        }
    }

    void FixedUpdate()
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        float currentSpeed = baseMoveSpeed * globalSpeedMultiplier;
        Vector2 movement = new Vector2(direction * currentSpeed * Time.fixedDeltaTime, 0f);
        rb.position += movement;

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

    // ÄNDERUNG 4: Skalierung des empfangenen Envelope-Werts
    public void SetGlobalSpeed(float envelopeValue)
    {
        // Mapped den eingehenden Wert auf den Bereich zwischen minSpeed und maxSpeed
        float calculatedSpeed = minSpeed + envelopeValue * (maxSpeed - minSpeed);
        targetSpeedMultiplier = Mathf.Clamp(calculatedSpeed, minSpeed, maxSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Sphere"))
        {
            FlipDirection(direction * -1);
            OnSphereCollision?.Invoke();

            if (pdInstance != null)
            {
                pdInstance.SendBang(ReceiverPdGo); 
                Debug.Log($"[Unity -> PD] Impuls für '{ReceiverPdGo}' auf {gameObject.name} gesendet.");
            }
        }
    }

    private void FlipDirection(int newDirection)
    {
        direction = newDirection;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (direction < 0);
        }
    }

    private void OnControlChangeCallback(int channel, int control, int value)
    {
        lastReceivedChannel = channel;
        lastReceivedCC = control;
        lastReceivedCCValue = value;

        if (channel == knobMidiChannel && control == targetCcControl)
        {
            float t = value / 127f;
            float calculatedSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

            targetSpeedMultiplier = calculatedSpeed;
        }
    }

    private void OnNoteOnCallback(int channel, int note, int velocity)
    {
        lastReceivedChannel = channel;
        lastReceivedNote = note;
        lastReceivedVelocity = velocity;

        if (channel == padMidiChannel && note == targetPadNote)
        {
            jumpRequested = true;
            pdPadBangRequested = true;
        }
        else if (channel == keyMidiChannel && note >= targetLowKeyNote && note <= targetHighKeyNote)
        {
            float t = Mathf.InverseLerp(targetLowKeyNote, targetHighKeyNote, note);
            float calculatedSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

            targetSpeedMultiplier = calculatedSpeed;
        }
    }
}