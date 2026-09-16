using UnityEngine;
using System;

public class CollisionPDPitchTrigg : MonoBehaviour
{
    public event Action OnSphereCollision;

    [Header("Pure Data")]
    [SerializeField] private LibPdInstance pdInstance;
    [SerializeField] private string ReceiverPdGo = "sahOn";
    [SerializeField] private string ReceiverFromKey = "sahOnKey";

    [Header("Global Speed Control")]
    [Range(0.1f, 20f)]
    public float globalSpeedMultiplier = 1f;
    [SerializeField] private float targetSpeedMultiplier = 1f;
    
    [Tooltip("Mindestgeschwindigkeit")]
    [SerializeField] private float minSpeed = 0.2f;
    [Tooltip("Maximalgeschwindigkeit")]
    [SerializeField] private float maxSpeed = 20.0f;
    [SerializeField] private float speedChangeSmoothness = 3f;
    [SerializeField] private bool MiceOnOff = true;

    [Header("Jump")] 
    [SerializeField] private float jumpForce = 10f;       
    [SerializeField] private bool jumpWithSpace = true;    

    [Header("Base Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 20f;
    public float leftBoundary = -11.5f;
    public float rightBoundary = 11.5f;

    [Header("Animation Settings")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    [Header("MIDI Input Source")] 
    [SerializeField] private MidiInput midiInput;

    [Header("Pad Settings (Jump / Bang)")]
    [SerializeField] private int padMidiChannel = 1;
    [SerializeField] private int targetPadNote = 36; 

    [Header("Keyboard Range (Tempo)")]
    [SerializeField] private int keyMidiChannel = 1;
    [SerializeField] private int targetLowKeyNote = 48; 
    [SerializeField] private int targetHighKeyNote = 72;

    [Header("Knob Settings (Tempo)")]
    [SerializeField] private int knobMidiChannel = 1;
    [SerializeField] private int targetCcControl = 1;

    [Header("Live MIDI Display")]
    [SerializeField] private int lastReceivedChannel;
    [SerializeField] private int lastReceivedNote;
    [SerializeField] private int lastReceivedVelocity;
    [SerializeField] private int lastReceivedCC;
    [SerializeField] private int lastReceivedCCValue;

    private Rigidbody2D rb;               
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float restY;                  
    private bool jumpRequested;           
    private bool pdPadBangRequested;
    private int direction = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        PdSpeedTriggReceiver.OnSpeedChanged += SetGlobalSpeed;
        PdSpeedTriggReceiver.OnJumpBang += OnPdJump;
        
        if (midiInput != null)
        {
            midiInput.onControlChange += OnControlChangeCallback;
            midiInput.onNoteOn += OnNoteOnCallback;
        }
    }

    void OnDisable()
    {
        PdSpeedTriggReceiver.OnSpeedChanged -= SetGlobalSpeed;
        PdSpeedTriggReceiver.OnJumpBang -= OnPdJump;
        
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
    }

    void Update()
    {
        globalSpeedMultiplier = Mathf.MoveTowards(
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
            TriggerJump();
        }

        if (pdPadBangRequested)
        {
            pdPadBangRequested = false;
            if (pdInstance != null)
            {
                pdInstance.SendBang(ReceiverFromKey);
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
        if (!MiceOnOff)
        {
            targetSpeedMultiplier = globalSpeedMultiplier;
            return;
        }
        targetSpeedMultiplier = Mathf.Clamp(multiplier, minSpeed, maxSpeed);
    }
    
    private void OnPdJump()
    {
        TriggerJump();
        Debug.Log($"[PD -> {gameObject.name}] PD-Bang empfangen & Sprung ausgeführt!");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Sphere"))
        {
            FlipDirection(direction * -1);
            OnSphereCollision?.Invoke();

            if (pdInstance != null)
            {
                pdInstance.SendBang(ReceiverPdGo);
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
            SetGlobalSpeed(calculatedSpeed);
        }
    }

    private void OnNoteOnCallback(int channel, int note, int velocity)
    {
        if (velocity <= 0) return;

        lastReceivedChannel = channel;
        lastReceivedNote = note;
        lastReceivedVelocity = velocity;

        if (channel == padMidiChannel && note == targetPadNote)
        {
            TriggerJump();
            pdPadBangRequested = true;
        }
        else if (channel == keyMidiChannel && note >= targetLowKeyNote && note <= targetHighKeyNote)
        {
            float t = Mathf.InverseLerp(targetLowKeyNote, targetHighKeyNote, note);
            float calculatedSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
            SetGlobalSpeed(calculatedSpeed);
        }
    }
}