using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CollisionPD))]
public class AudioPositionFilter : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClip;

    [Header("Cutoff Frequenzen")]
    [Tooltip("Frequenz ganz rechts (Filter geschlossen, nur Tiefen hörbar)")]
    [SerializeField] private float minCutoffHz = 200f;

    [Tooltip("Frequenz ganz links (Filter offen, Höhen voll hörbar)")]
    [SerializeField] private float maxCutoffHz = 12000f;

    [Tooltip("Filter-Resonanz (Q-Faktor: 0.707 = neutral/Butterworth)")]
    [Range(0.1f, 4.0f)]
    [SerializeField] private float resonanceQ = 0.707f;

    // Referenzen & DSP-Variablen
    private AudioSource audioSource;
    private CollisionPD collisionPD;
    private float sampleRate = 44100f;

    private float currentCutoffHz = 1000f;
    private float targetCutoffHz = 1000f;

    // State Variable Filter State (Stereo)
    private float[] ic1eq = new float[2];
    private float[] ic2eq = new float[2];

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        collisionPD = GetComponent<CollisionPD>();
    }

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;

        if (audioSource != null && audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (collisionPD == null) return;

        // 1. Position von 0.0 (Links) bis 1.0 (Rechts) berechnen
        float currentX = transform.position.x;
        float normalizedX = Mathf.InverseLerp(collisionPD.leftBoundary, collisionPD.rightBoundary, currentX);
        normalizedX = Mathf.Clamp01(normalizedX);

        // 2. Invertierte logarithmische Skalierung:
        // normalizedX = 0 (Links)  -> Cutoff = maxCutoffHz (12.000 Hz, offen)
        // normalizedX = 1 (Rechts) -> Cutoff = minCutoffHz (200 Hz, nur Tiefen)
        targetCutoffHz = maxCutoffHz * Mathf.Pow(minCutoffHz / maxCutoffHz, normalizedX);

        // 3. Glättung im Hauptthread
        currentCutoffHz = Mathf.Lerp(currentCutoffHz, targetCutoffHz, Time.deltaTime * 15f);
    }

    /// <summary>
    /// Audio Thread DSP
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        // Sicherheits-Clamp gegen Filter-Absturz (NaN)
        float safeCutoff = Mathf.Clamp(currentCutoffHz, 20f, sampleRate * 0.42f);

        // SVF Filterkoeffizienten
        float f = Mathf.Tan(Mathf.PI * safeCutoff / sampleRate);
        float k = 1f / resonanceQ;
        float a1 = 1f / (1f + f * (f + k));
        float a2 = f * a1;
        float a3 = f * a2;

        int numChannels = Mathf.Min(channels, 2);

        for (int i = 0; i < data.Length; i += channels)
        {
            for (int ch = 0; ch < numChannels; ch++)
            {
                float inSample = data[i + ch];

                // State Variable Low-Pass Filter
                float v3 = inSample - ic2eq[ch];
                float v1 = a1 * ic1eq[ch] + a2 * v3;
                float v2 = ic2eq[ch] + a2 * ic1eq[ch] + a3 * v3;

                ic1eq[ch] = 2f * v1 - ic1eq[ch];
                ic2eq[ch] = 2f * v2 - ic2eq[ch];

                // Ausgabe des Low-Pass Signals
                data[i + ch] = v2;
            }
        }
    }
}