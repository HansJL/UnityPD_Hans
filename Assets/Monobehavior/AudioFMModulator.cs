using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CollisionPD))]
public class AudioFMModulator : MonoBehaviour
{
    public enum WaveformType
    {
        Sinus,
        Triangle,
        Sawtooth,
        Square
    }

    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClipToModulate;
    [SerializeField] private float maxPitchAtLeft = 2.0f;
    [SerializeField] private float minPitchAtRight = 0.5f;

    [Header("FM Waveform Selection")]
    [Tooltip("Wellenform 1 (Träger / Carrier)")]
    [SerializeField] private WaveformType carrierWave = WaveformType.Sinus;
    
    [Tooltip("Wellenform 2 (Modulator)")]
    [SerializeField] private WaveformType modulatorWave = WaveformType.Sinus;

    [Header("FM Frequency Settings")]
    [Tooltip("Frequenz der Trägerwelle in Hz")]
    [Range(0.1f, 1000f)]
    [SerializeField] private float carrierFrequency = 220f;

    [Tooltip("Frequenz der Modulationswelle in Hz")]
    [Range(0.1f, 1000f)]
    [SerializeField] private float modulatorFrequency = 110f;

    [Header("FM Modulation Parameters")]
    [Tooltip("Stärke der Frequenzmodulation (Modulationsindex)")]
    [Range(0f, 5000f)]
    [SerializeField] private float modulationIndex = 100f;

    [Tooltip("Mix zwischen Originalton (0) und Moduliertem Ton (1)")]
    [Range(0f, 1f)]
    [SerializeField] private float modulationDepth = 0.8f;

    // Referenzen & DSP-Variablen
    private AudioSource audioSource;
    private CollisionPD collisionPD;
    
    private float carrierPhase = 0f;
    private float modulatorPhase = 0f;
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
    /// Audio-DSP Thread: Verarbeitet die FM-Modulation mit multiplizierten Wellenformen
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        float phaseIncrement = 1f / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            // 1. Modulator-Signal berechnen
            float modValue = EvaluateWaveform(modulatorWave, modulatorPhase, modulatorFrequency);
            
            // 2. Trägersignal berechnen
            float carrierValue = EvaluateWaveform(carrierWave, carrierPhase, carrierFrequency);

            // 3. Multiplikation der beiden Wellenformen
            float combinedModulation = carrierValue * modValue;

            // 4. FM-Synthese: Frequenz/Phasenverschiebung auf das Audiosignal anwenden
            // Das multiplizierte Signal steuert den Modulationsauslenkwinkel
            float fmPhaseOffset = combinedModulation * modulationIndex * phaseIncrement;

            for (int channel = 0; channel < channels; channel++)
            {
                float originalSample = data[i + channel];
                
                // Anwendung des phasenmodulierten Signals auf das Audiosample
                float modulatedSample = originalSample * Mathf.Sin(2f * Mathf.PI * (carrierPhase + fmPhaseOffset));

                // Wet/Dry Mix
                data[i + channel] = Mathf.Lerp(originalSample, modulatedSample, modulationDepth);
            }

            // Phaseninkrementation für beide Oszillatoren
            carrierPhase += phaseIncrement;
            if (carrierPhase >= 1f) carrierPhase -= 1f;

            modulatorPhase += phaseIncrement;
            if (modulatorPhase >= 1f) modulatorPhase -= 1f;
        }
    }

    /// <summary>
    /// Wertet die gewählte Wellenform über die ControlFunctions aus
    /// </summary>
    private float EvaluateWaveform(WaveformType type, float t, float freq)
    {
        switch (type)
        {
            case WaveformType.Triangle:
                return ControlFunctions.Tri(t, freq, 0f);
            case WaveformType.Sawtooth:
                return ControlFunctions.Saw(t, freq, 0f);
            case WaveformType.Square:
                return ControlFunctions.Squ(t, freq, 0f);
            case WaveformType.Sinus:
            default:
                return ControlFunctions.Sin(t, freq, 0f);
        }
    }
}
