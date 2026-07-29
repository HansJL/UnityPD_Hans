using UnityEngine;
using System;

public class CollisionPD : MonoBehaviour
{
    // Referenz auf die Pure Data Schnittstelle (LibPD)
    [Header("Pure Data")]
    [SerializeField] private LibPdInstance pdInstance;

    [Header("Receiver")] 
    [SerializeField] private string ReceiverPdGo = "sahOn";
    
    [Header("Receiver Keyboard")] 
    
    [SerializeField] private string ReceiverFromKey = "sahOn";
    
    [SerializeField] private int midichannel = 9;
    
    [SerializeField] private int noteMidi = 40;
    
    //midichannel
    [SerializeField] private MidiInput midiInput; // Referenz auf das Skript, das MIDI-Signale empfängt

    // Komponenten und Variablen für die Physik-Steuerung
    private Rigidbody2D rb;               // 2D-Physikkomponente der Spielfigur
    private AudioSource audioSource;      // (Aktuell ungenutzt) Optionale AudioSource
    private float restY;                  // Start-Höhe (Y-Position) als Boden-Grenze
    private bool jumpRequested;           // Merker, ob der Spieler die Sprungtaste gedrückt hat

    // Callbacks (Funktions-Zeiger) für MIDI-Events
    private Action<int, int, int> onControlChangeCallback;
    private Action<int, int, int> onNoteOnCallback;
   
    [Header("Jump")] 
    [SerializeField] private float jumpForce = 10f;       // Sprungkraft der Spielfigur
    [SerializeField] private bool jumpWithSpace = true;    // Soll die Leertaste den Sprung auslösen?
    
  
    // Awake wird ganz zu Beginn beim Laden des Objekts aufgerufen
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true; // Verhindert, dass die Figur bei Kollisionen umkippt

        // Speichert die Methoden-Referenzen für die MIDI-Events
        onControlChangeCallback = OnControlChangeCallback;
        onNoteOnCallback = OnNoteOnCallback;
    }

    // OnEnable wird ausgeführt, wenn das Objekt aktiv wird
    void OnEnable()
    {
        // Abonnieren der MIDI-Events (verknüpft das MidiInput-Skript mit unseren Methoden)
        if (midiInput != null)
        {
            midiInput.onControlChange += onControlChangeCallback;
            midiInput.onNoteOn += onNoteOnCallback;
        }
    }

    // OnDisable wird ausgeführt, wenn das Objekt deaktiviert wird
    void OnDisable()
    {
        // De-Abonnieren der MIDI-Events, um Speicherlecks und Fehler zu vermeiden
        if (midiInput != null)
        {
            midiInput.onControlChange -= onControlChangeCallback;
            midiInput.onNoteOn -= onNoteOnCallback;
        }
    }

    // Start wird vor dem ersten Frame aufgerufen
    void Start()
    {
        restY = transform.position.y;
    if (pdInstance == null)
        pdInstance = GetComponentInChildren<LibPdInstance>();
    if (pdInstance == null)
        Debug.LogError($"{name}: LibPdInstance not assigned and not found on prefab!");
    }

    // Update wird in jedem Frame aufgerufen (für Input-Abfragen)
    void Update()
    {
        // Prüft, ob die Leertaste oder die Sprungtaste gedrückt wurde
        if (jumpWithSpace && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")))
        {
            jumpRequested = true; // Registriert den Sprungwunsch für den nächsten Physik-Frame
        }
    }

    // FixedUpdate wird in festen Physik-Intervallen aufgerufen (für Bewegung/Physik)
    void FixedUpdate()
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        // Führt den Sprung aus, falls in Update() die Leertaste gedrückt wurde
        if (jumpRequested)
        {
            jumpRequested = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Verhindert, dass die Figur unter ihre ursprüngliche Start-Höhe (restY) fällt
        if (rb.linearVelocity.y <= 0f && rb.position.y < restY)
        {
            rb.position = new Vector2(rb.position.x, restY);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    // Wird automatisch von Unity aufgerufen, wenn dieser Collider ein anderes Objekt berührt
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Prüft, ob das getroffene Objekt den Tag "Sphere" hat
        if (collision.gameObject.CompareTag("Sphere"))
        {
            if (pdInstance != null)
            {
                // Schickt einen Bang an den Receiver [r collision] in Pure Data
                //pdInstance.SendBang("collision"); 

                // Schickt einen zweiten Bang an den Receiver [r sahOn] in Pure Data
                pdInstance.SendBang(ReceiverPdGo); 

                Debug.Log("[Unity] Impuls für collision & sahOn an Pure Data geschickt.");
            }
        }
    }

    // Wird aufgerufen, wenn ein MIDI Controller Change (z. B. Drehregler/Pads) empfangen wird
    void OnControlChangeCallback(int channel, int control, int value)
    {
        // Reagiert auf Kanal 1, Control-Nummer 71, wenn der Wert größer als 0 ist
        if (channel == 1 && control == 60 && value > 0)
        {
            if (pdInstance != null)
            {
                Debug.Log("[Unity] Pad touch for CC71 detected.");
            }
        }
    }

    // Wird aufgerufen, wenn eine MIDI Note (z. B. Tastenanschlag/Pad) empfangen wird
    void OnNoteOnCallback(int channel, int note, int velocity)
    {
        // Reagiert speziell auf Kanal 9, Note 40 (oft ein Drum-Pad) bei Anschlagstärke > 0
        if (channel == midichannel && note == noteMidi && velocity > 0)

   
        {
         // Sprung auslösen
        jumpRequested = true;

            if (pdInstance != null)
            {
                // Schickt den Zahlenwert 127 als Float an den Receiver [r sahOn] in Pure Data
                //pdInstance.SendFloat(ReceiverPdKey, 127f);
                pdInstance.SendBang(ReceiverFromKey);
                Debug.Log("[Unity] Sah on detected via MIDI Note 40.");



            }
        }
    }
}