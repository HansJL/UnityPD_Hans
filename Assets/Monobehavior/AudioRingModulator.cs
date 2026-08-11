using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CollisionPD))]
public class AudioRingModulator : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClipToModulate;
    [SerializeField] private float maxPitchAtLeft = 2.0f;
    [SerializeField] private float minPitchAtRight = 0.5f;

    [Header("Waveform Selector")]
    [SerializeField] private bool useSinus = true;
    [SerializeField] private bool useTriangle = false;
    [SerializeField] private bool useSawtooth = false;
    [SerializeField] private bool useSquare = false;

    [Header("Ring Modulation")]
    [Tooltip("Basis-Frequenz des Modulators in Hz")]
    [SerializeField] private float modulationFrequency = 220f;
    [Tooltip("Mix zwischen Originalton (0) und Moduliertem Ton (1)")]
    [Range(0f, 1f)]
    [SerializeField] private float modulationDepth = 0.8f;

    // Referenzen & DSP-Variablen
    private AudioSource audioSource;
    private CollisionPD collisionPD;
    private float audioPhase = 0f;
    private float sampleRate = 44100f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        collisionPD = GetComponent<CollisionPD>();
    }

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;

        if (audioSource != null && audioClipToModulate != null)
        {
            audioSource.clip = audioClipToModulate;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (collisionPD == null || audioSource == null) return;

        // X-Position basierend auf den Boundaries von CollisionPD berechnen (0.0 bis 1.0)
        float currentX = transform.position.x;
        float normalizedX = Mathf.InverseLerp(collisionPD.leftBoundary, collisionPD.rightBoundary, currentX);

        // Pitch an die Position koppeln (Links = Hoch, Rechts = Tief)
        audioSource.pitch = Mathf.Lerp(maxPitchAtLeft, minPitchAtRight, normalizedX);
    }

    /// <summary>
    /// Audio-DSP Thread: Verarbeitet die Ringmodulation unabhängig vom Haupt-Thread
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        float phaseIncrement = 1f / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            // Berechnet das Modulationssignal mit den ControlFunctions
            float modSignal = GetModulatorValue(audioPhase, modulationFrequency);

            for (int channel = 0; channel < channels; channel++)
            {
                float originalSample = data[i + channel];
                float modulatedSample = originalSample * modSignal;

                // Wet/Dry Mix
                data[i + channel] = Mathf.Lerp(originalSample, modulatedSample, modulationDepth);
            }

            audioPhase += phaseIncrement;
            if (audioPhase >= 1f) audioPhase -= 1f;
        }
    }

    private float GetModulatorValue(float t, float freq)
    {
        if (useSquare)
            return ControlFunctions.Squ(t, freq, 0f);
        if (useSawtooth)
            return ControlFunctions.Saw(t, freq, 0f);
        if (useTriangle)
            return ControlFunctions.Tri(t, freq, 0f);

        return ControlFunctions.Sin(t, freq, 0f);
    }
}