using System;
using UnityEngine;

public class PdSpeedTriggReceiver : MonoBehaviour
{
    [Header("Pure Data")]
    [SerializeField] private LibPdInstance pdInstance;

    // ------------------------------------------------------------
    // FLOAT AUSWAHL
    // ------------------------------------------------------------

    public enum ReceiverType
    {
        Off,
        EnvelopeFollow,
        PitchTracking
    }

    [Header("Float Receiver")]
    [SerializeField] private ReceiverType receiverType =
        ReceiverType.PitchTracking;

    // ------------------------------------------------------------
    // PITCH
    // ------------------------------------------------------------

    [Header("Pitch Tracking")]
    [SerializeField] private float speedMultiplier = 10f;

    // ------------------------------------------------------------
    // ENVELOPE
    // ------------------------------------------------------------

    [Header("Envelope Follow")]
    [SerializeField] private float envelopeMultiplier = 10f;

    [SerializeField] private bool useSqrtForSensitivity = false;

    // ------------------------------------------------------------
    // TRIGGER
    // ------------------------------------------------------------

    [Header("Trigger")]
    [SerializeField] private bool useTrigger = true;

    // ------------------------------------------------------------
    // EVENTS
    // ------------------------------------------------------------

    public static event Action<float> OnSpeedChanged;
    public static event Action<float> OnEnvelopeChanged;
    public static event Action OnJumpBang;

    // ------------------------------------------------------------
    // START
    // ------------------------------------------------------------

    private void Start()
    {
        if (pdInstance == null)
            pdInstance = GetComponent<LibPdInstance>();

        if (pdInstance == null)
        {
            Debug.LogError(
                "[PdSpeedTriggReceiver] LibPdInstance fehlt!"
            );
            return;
        }

        // Alle drei PD-Receiver einmal binden
        pdInstance.Bind("trigger");
        pdInstance.Bind("pitch_tracking");
        pdInstance.Bind("envelope_follow");

        // Listener
        pdInstance.pureDataEvents.Float.AddListener(OnReceiveFloat);
        pdInstance.pureDataEvents.Bang.AddListener(OnReceiveBang);
    }

    // ------------------------------------------------------------
    // RUNTIME: FLOAT RECEIVER WECHSELN
    // ------------------------------------------------------------

    public void SetReceiver(ReceiverType newReceiver)
    {
        receiverType = newReceiver;

        Debug.Log(
            "[PdSpeedTriggReceiver] Receiver: " + newReceiver
        );
    }

    // ------------------------------------------------------------
    // RUNTIME: TRIGGER EIN/AUS
    // ------------------------------------------------------------

    public void SetTrigger(bool enabled)
    {
        useTrigger = enabled;
    }

    // ------------------------------------------------------------
    // FLOAT VON PURE DATA
    // ------------------------------------------------------------

    private void OnReceiveFloat(string receiver, float value)
    {
        // ENVELOPE
        if (receiverType == ReceiverType.EnvelopeFollow &&
            receiver == "envelope_follow")
        {
            float normalized = Mathf.Clamp01(value);

            if (useSqrtForSensitivity)
                normalized = Mathf.Sqrt(normalized);

            float rawValue =
                normalized * envelopeMultiplier;

            OnEnvelopeChanged?.Invoke(rawValue);
            Debug.Log("SpeedChanged Envelope: " + rawValue);
            return;
        }

        // PITCH
        if (receiverType == ReceiverType.PitchTracking &&
            receiver == "pitch_tracking")
        {
            float normalized = Mathf.Clamp01(value);

            float curvedValue =
                Mathf.Pow(normalized, 3f);

            float rawValue =
                curvedValue * speedMultiplier;

            OnSpeedChanged?.Invoke(rawValue);
            
            Debug.Log("SpeedChanged Pitch: " + rawValue);

            return;
        }
    }

    // ------------------------------------------------------------
    // BANG VON PURE DATA
    // ------------------------------------------------------------

    private void OnReceiveBang(string receiver)
    {
        if (useTrigger &&
            receiver == "trigger")
        {
            OnJumpBang?.Invoke();
        }
    }

    // ------------------------------------------------------------
    // DESTROY
    // ------------------------------------------------------------

    private void OnDestroy()
    {
        if (pdInstance == null)
            return;

        pdInstance.pureDataEvents.Float.RemoveListener(
            OnReceiveFloat
        );

        pdInstance.pureDataEvents.Bang.RemoveListener(
            OnReceiveBang
        );

        pdInstance.UnBind("trigger");
        pdInstance.UnBind("pitch_tracking");
        pdInstance.UnBind("envelope_follow");
    }

}