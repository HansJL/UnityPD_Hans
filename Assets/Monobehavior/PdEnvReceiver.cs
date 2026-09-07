using System;
using UnityEngine;

public class PdEnvReceiver : MonoBehaviour
{
    [Header("Pure Data Settings")]
    [SerializeField] private LibPdInstance pdInstance;
    [SerializeField] private string receiverName = "envelope_follow"; // Exakt wie [s envelope_follow] in PD

    [Header("Speed Multiplier")]
    [SerializeField] private float speedMultiplier = 10.0f;
    
    [Tooltip("Optional: Zieht die Wurzel aus dem PD-Wert, um leises Singen reaktiver zu machen.")]
    [SerializeField] private bool useSqrtForSensitivity = false;

    // Statisches Event für den Envelope Follower
    public static event Action<float> OnEnvelopeChanged;
    private bool isBound = false;

    void Start()
    {
        if (pdInstance != null)
        {
            try
            {
                pdInstance.Bind(receiverName);
                isBound = true;
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"[PdEnvReceiver] '{receiverName}' war bereits an LibPdInstance gebunden.");
            }

            pdInstance.pureDataEvents.Float.AddListener(OnReceiveFloat);
        }
        else
        {
            Debug.LogError("[PdEnvReceiver] Bitte LibPdInstance im Inspector zuweisen!");
        }
    }

    void OnDestroy()
    {
        if (pdInstance != null)
        {
            if (isBound)
            {
                pdInstance.UnBind(receiverName);
                isBound = false;
            }

            pdInstance.pureDataEvents.Float.RemoveListener(OnReceiveFloat);
        }
    }

    private void OnReceiveFloat(string receiver, float value)
    {
        if (receiver == receiverName)
        {
            // 1. Wert absichern (Durch Quadrieren in PD bereits positiv 0..1)
            float normalized = Mathf.Clamp01(value);

            // 2. Optional: Leise Signale anheben
            if (useSqrtForSensitivity)
            {
                normalized = Mathf.Sqrt(normalized);
            }

            // 3. Mit Multiplikator verrechnen und Event auslösen
            float rawValue = normalized * speedMultiplier;            
            OnEnvelopeChanged?.Invoke(rawValue);
        }
    }
}