using System;
using UnityEngine;


public class PdSpeedReceiver : MonoBehaviour
{
    [Header("Pure Data Settings")]
    [SerializeField] private LibPdInstance pdInstance;
    [SerializeField] private string receiverName = "pitch_tracking";

    [Header("Speed Multiplier")]
    [SerializeField] private float speedMultiplier = 10.0f;

    // Static Event: Schickt den PD-Wert (0.0 bis 1.0) direkt an alle Abonnenten
    public static event Action<float> OnSpeedChanged;

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
                Debug.LogWarning($"[PdSpeedReceiver] '{receiverName}' war bereits an LibPdInstance gebunden.");
            }

            pdInstance.pureDataEvents.Float.AddListener(OnReceiveFloat);
        }
        else
        {
            Debug.LogError("[PdSpeedReceiver] Bitte LibPdInstance im Inspector zuweisen!");
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
        // Zeigt JEDE eingehende Nachricht im C#-Receiver an
        // Debug.Log($"<color=green>[C# EMPFANG]</color> Receiver: '{receiver}' | Wert: {value}");
        
        if (receiver == receiverName)
        {
            
            float rawValue = value * speedMultiplier;
            // Wert direkt ohne Verzögerung an CollisionPD abfeuern
            OnSpeedChanged?.Invoke(rawValue);
        }
    }
}