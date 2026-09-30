using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Bridges EnvPitchTrigSender.pd sends into Unity events.
/// Bind once per LibPdInstance; put this on the character (or a dedicated bridge object).
/// </summary>
public class PdMicBridge : MonoBehaviour
{
    public enum SpeedSource
    {
        Off,
        EnvelopeFollow,
        PitchTracking
    }

    [Header("Pure Data")]
    [SerializeField] private LibPdInstance pdInstance;

    [Header("Continuous Speed Source")]
    [SerializeField] private SpeedSource speedSource = SpeedSource.PitchTracking;

    [Header("Pitch Tracking")]
    [SerializeField] private float pitchSpeedMultiplier = 10f;

    [Header("Envelope Follow")]
    [SerializeField] private float envelopeSpeedMultiplier = 10f;
    [SerializeField] private bool useSqrtForSensitivity = false;

    [Header("Trigger (clap / transient → jump)")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("When enabled, bangs are ignored unless recent envelope is above the threshold. Leave off for reliable clap jumps.")]
    [SerializeField] private bool useVolumeThreshold = false;
    [Range(0f, 1f)]
    [SerializeField] private float triggerVolumeThreshold = 0.02f;

    public static event Action<float> OnSpeedChanged;
    public static event Action OnJumpBang;

    private float latestEnvelopeValue;
    private bool isBound;

    private void Start()
    {
        if (pdInstance == null)
            pdInstance = GetComponent<LibPdInstance>()
                ?? GetComponentInParent<LibPdInstance>()
                ?? FindFirstObjectByType<LibPdInstance>();

        if (pdInstance == null)
        {
            Debug.LogError($"[{nameof(PdMicBridge)}] No LibPdInstance assigned on {name}.");
            return;
        }

        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        float timeout = 5f;
        float start = Time.realtimeSinceStartup;

        while (!pdInstance.IsLoaded && Time.realtimeSinceStartup - start < timeout)
            yield return null;

        if (!pdInstance.IsLoaded)
        {
            Debug.LogError($"[{nameof(PdMicBridge)}] LibPdInstance on '{pdInstance.name}' never loaded.");
            yield break;
        }

        TryBind("trigger");
        TryBind("pitch_tracking");
        TryBind("envelope_follow");
        isBound = true;

        pdInstance.pureDataEvents.Float.AddListener(OnReceiveFloat);
        pdInstance.pureDataEvents.Bang.AddListener(OnReceiveBang);

        Debug.Log($"[{nameof(PdMicBridge)}] Bound envelope_follow / pitch_tracking / trigger on {pdInstance.name}.");
    }

    public void SetSpeedSource(SpeedSource source)
    {
        speedSource = source;
    }

    public void SetTriggerEnabled(bool enabled)
    {
        useTrigger = enabled;
    }

    private void TryBind(string symbol)
    {
        try
        {
            pdInstance.Bind(symbol);
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"[{nameof(PdMicBridge)}] '{symbol}' was already bound on {pdInstance.name}.");
        }
    }

    private void OnReceiveFloat(string receiver, float value)
    {
        if (receiver == "envelope_follow")
            latestEnvelopeValue = Mathf.Clamp01(value);

        if (speedSource == SpeedSource.EnvelopeFollow && receiver == "envelope_follow")
        {
            float normalized = latestEnvelopeValue;
            if (useSqrtForSensitivity)
                normalized = Mathf.Sqrt(normalized);

            OnSpeedChanged?.Invoke(normalized * envelopeSpeedMultiplier);
            return;
        }

        if (speedSource == SpeedSource.PitchTracking && receiver == "pitch_tracking")
        {
            float normalized = Mathf.Clamp01(value);
            float curved = Mathf.Pow(normalized, 3f);
            OnSpeedChanged?.Invoke(curved * pitchSpeedMultiplier);
        }
    }

    private void OnReceiveBang(string receiver)
    {
        if (!useTrigger || receiver != "trigger")
            return;

        if (useVolumeThreshold && latestEnvelopeValue < triggerVolumeThreshold)
            return;

        OnJumpBang?.Invoke();
    }

    private void OnDestroy()
    {
        if (pdInstance == null || !isBound)
            return;

        pdInstance.pureDataEvents.Float.RemoveListener(OnReceiveFloat);
        pdInstance.pureDataEvents.Bang.RemoveListener(OnReceiveBang);

        pdInstance.UnBind("trigger");
        pdInstance.UnBind("pitch_tracking");
        pdInstance.UnBind("envelope_follow");
    }
}
