using UnityEngine;

[RequireComponent(typeof(PlayerCollisionAndMovement))]
public class PlayerMidiController : MonoBehaviour
{
    [Header("MIDI Input Source")]
    public MidiInput midiInput;
    public string receiverFromKey = "sahOnKey";

    [Header("Pad Settings")]
    public int padMidiChannel = 1;
    public int targetPadNote = 36;

    [Header("Keyboard Range")]
    public int keyMidiChannel = 1;
    public int targetLowKeyNote = 48;
    public int targetHighKeyNote = 72;

    [Header("Knob Settings")]
    public int knobMidiChannel = 1;
    public int targetCcControl = 1;

    [Header("Live MIDI Display (Nur zum Ablesen)")]
    public int lastReceivedChannel;
    public int lastReceivedNote;
    public int lastReceivedVelocity;
    public int lastReceivedCC;
    public int lastReceivedCCValue;

    private PlayerCollisionAndMovement playerMovement;

    void Awake()
    {
        playerMovement = GetComponent<PlayerCollisionAndMovement>();
    }

    void OnEnable()
    {
        if (midiInput != null)
        {
            midiInput.onControlChange += OnControlChangeCallback;
            midiInput.onNoteOn += OnNoteOnCallback;
            Debug.Log($"[PlayerMidiController] MIDI-Events auf {gameObject.name} registriert.");
        }
        else
        {
            Debug.LogWarning($"[PlayerMidiController] {name}: MidiInput ist im Inspector NICHT zugewiesen!");
        }
    }

    void OnDisable()
    {
        if (midiInput != null)
        {
            midiInput.onControlChange -= OnControlChangeCallback;
            midiInput.onNoteOn -= OnNoteOnCallback;
        }
    }

    private void OnControlChangeCallback(int channel, int control, int value)
    {
        // 1. Live-Anzeige im Inspector aktualisieren
        lastReceivedChannel = channel;
        lastReceivedCC = control;
        lastReceivedCCValue = value;

        Debug.Log($"[MIDI CC EMPFANGEN] Kanal: {channel} | CC: {control} | Wert: {value}");

        // 2. Abfragen auf den Knobs-Kanal beschränken
        if (channel == knobMidiChannel && control == targetCcControl)
        {
            float t = value / 127f;
            float calculatedSpeed = Mathf.Lerp(playerMovement.minSpeed, playerMovement.maxSpeed, t);

            playerMovement.SetGlobalSpeed(calculatedSpeed);
            Debug.Log($"[AKTION] Knob CC {control} (Kanal {channel}, Wert: {value}) -> Tempo auf {calculatedSpeed:F2} gesetzt.");
        }
    }

    private void OnNoteOnCallback(int channel, int note, int velocity)
    {
        // 1. Live-Anzeige im Inspector aktualisieren
        lastReceivedChannel = channel;
        lastReceivedNote = note;
        lastReceivedVelocity = velocity;

        Debug.Log($"[MIDI NOTE EMPFANGEN] Kanal: {channel} | Note: {note} | Velocity: {velocity}");

        // 2. Pad-Verarbeitung (Sprung & Pure Data Bang)
        if (channel == padMidiChannel && note == targetPadNote)
        {
            playerMovement.TriggerJump();

            if (playerMovement.pdInstance != null)
            {
                playerMovement.pdInstance.SendBang(receiverFromKey);
                Debug.Log("[Unity -> PD] Bang für Pad gedrückt gesendet.");
            }

            Debug.Log($"[AKTION] Pad-Note ({note}) auf Kanal {channel} erkannt -> Sprung!");
        }
        // 3. Keyboard-Verarbeitung (Tempo per Taste)
        else if (channel == keyMidiChannel && note >= targetLowKeyNote && note <= targetHighKeyNote)
        {
            float t = Mathf.InverseLerp(targetLowKeyNote, targetHighKeyNote, note);
            float calculatedSpeed = Mathf.Lerp(playerMovement.minSpeed, playerMovement.maxSpeed, t);

            playerMovement.SetGlobalSpeed(calculatedSpeed);
            Debug.Log($"[AKTION] Note {note} (Kanal {channel}) gespielt -> Tempo auf {calculatedSpeed:F2} gesetzt.");
        }
    }
}