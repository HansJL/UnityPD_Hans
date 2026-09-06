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
        // 1. Sicheren Bereich garantieren (0 bis 1)
        float normalized = Mathf.Clamp01(value);

        // 2. Exponentielle Kurve anwenden (z.B. Potenz 3.0f):
        // Tiefe Töne flachen stark ab (sehr langsam), hohe Töne steigen steil an (sehr schnell)
        float curvedValue = Mathf.Pow(normalized, 3.0f);

        // 3. Mit dem Multiplikator verrechnen
        float rawValue = curvedValue * speedMultiplier;            

// value = Mathf.Sqrt(value) +0.5f;
            // value = (Mathf.Pow(2f, (value + 1f) * 3.0f) - 8f) / 20f;
           // Debug.Log($"[PdSpeedReceiver] Received Value: {value}");

            // float clampedValue = Mathf.Clamp01(value);
            //float rawValue = value * speedMultiplier;
            // Wert direkt ohne Verzögerung an CollisionPD abfeuern
            OnSpeedChanged?.Invoke(rawValue);

        // Value zwische 0 und 1
       // float clampedValue = Mathf.Clamp01(value);
      //    OnSpeedChanged?.Invoke(rawValue);
        }
    }
}