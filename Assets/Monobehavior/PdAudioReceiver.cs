using System;
using System.Collections.Generic;
using UnityEngine;

public class PdAudioReceiver : MonoBehaviour
{
    [Header("Pure Data Settings")]
    [SerializeField] private LibPdInstance pdInstance;

    // Static Event für den ausgewählten Wert
    public static event Action<float> OnSpeedChanged;

    private string currentBoundReceiver = "";

    void Start()
    {
        if (pdInstance == null)
        {
            pdInstance = GetComponentInParent<LibPdInstance>();
        }

        if (pdInstance == null)
        {
            Debug.LogError("[PdAudioReceiver] Keine LibPdInstance gefunden!");
        }
    }

    void OnDestroy()
    {
        UnbindCurrent();
    }

    /// <summary>
    /// Wird von CollisionPD aufgerufen, wenn der Modus gewechselt wird.
    /// </summary>
    public void SwitchReceiver(string newReceiverName)
    {
        if (pdInstance == null || string.IsNullOrEmpty(newReceiverName)) return;

        // Wenn bereits gebunden, alten Receiver lösen
        UnbindCurrent();

        try
        {
            pdInstance.Bind(newReceiverName);
            currentBoundReceiver = newReceiverName;
            
            // Event-Listener sicherstellen
            pdInstance.pureDataEvents.Float.RemoveListener(OnReceiveFloat);
            pdInstance.pureDataEvents.Float.AddListener(OnReceiveFloat);

            Debug.Log($"[PdAudioReceiver] Erfolgreich an '{newReceiverName}' gebunden.");
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"[PdAudioReceiver] '{newReceiverName}' war bereits gebunden.");
        }
    }

    private void UnbindCurrent()
    {
        if (pdInstance != null && !string.IsNullOrEmpty(currentBoundReceiver))
        {
            pdInstance.UnBind(currentBoundReceiver);
            pdInstance.pureDataEvents.Float.RemoveListener(OnReceiveFloat);
            currentBoundReceiver = "";
        }
    }

    private void OnReceiveFloat(string receiver, float value)
    {
        // Nur verarbeiten, wenn die Nachricht vom aktuell gewählten Receiver kommt
        if (receiver == currentBoundReceiver)
        {
            OnSpeedChanged?.Invoke(value);
        }
    }
}