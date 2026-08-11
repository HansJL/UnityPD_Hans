using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CollisionPD))]
public class Audio2FMModulator : MonoBehaviour
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

    [Header("Waveform Selection")]
    [Tooltip("Trägerwelle (Gesteuert durch Bewegung)")]
    [SerializeField] private WaveformType carrierWave = WaveformType.Sinus;
    
    [Tooltip("Modulationswelle (Gesteuert durch Schieberegler)")]
    [SerializeField] private WaveformType modulatorWave = WaveformType.Sinus;

    [Header("Carrier Frequency (Controlled by Position)")]
    [Tooltip("Links: 3 Hz = Sauberes, tiefes LFO/Vibrato")]
    [Range(0.1f, 100f)]
    [SerializeField] private float minCarrierFreqAtLeft = 3f;
    
    [Tooltip("Rechts: 60 Hz = Schöner, warmer FM-Sound")]
    [Range(0.1f, 100f)]
    [SerializeField] private float maxCarrierFreqAtRight = 60f;

    [Header("Modulator Frequency (Controlled by Slider)")]
    [Tooltip("Frequenz der Modulationswelle in Hz")]
    [Range(0.1f, 100f)]
    [SerializeField] private float modulatorFrequency = 2f;

    [Header("FM Parameters")]
    [Tooltip("Wie stark die Frequenz abweicht (Modulationsindex)")]
    [Range(0f, 1f)]
    [SerializeField] private float modulationDepth = 0.5f;

    // Referenzen & DSP-Variablen
    private AudioSource audioSource;
    private CollisionPD collisionPD;
    
    private float currentCarrierFrequency = 3f;
    private float carrierPhase = 0f;
    private float modulatorPhase = 0f;
    private float sampleRate = 44100f;

    // Direct Buffer für saubere Phasenmodulation
    private float[] ringBuffer = new float[8192];
    private int bufferWriteHead = 0;

    private Dictionary<WaveformType, Func<float, float, float>> waveformFuncs;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        collisionPD = GetComponent<CollisionPD>();

        waveformFuncs = new Dictionary<WaveformType, Func<float, float, float>>
        {
            { WaveformType.Sinus,    (t, freq) => ControlFunctions.Sin(t, freq, 0f) },
            { WaveformType.Triangle, (t, freq) => ControlFunctions.Tri(t, freq, 0f) },
            { WaveformType.Sawtooth, (t, freq) => ControlFunctions.Saw(t, freq, 0f) },
            { WaveformType.Square,   (t, freq) => ControlFunctions.Squ(t, freq, 0f) }
        };
    }

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;

        if (audioSource != null && audioClipToModulate != null)
        {
            audioSource.clip = audioClipToModulate;
            audioSource.loop = true;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (collisionPD == null) return;

        // Position normieren
        float currentX = transform.position.x;
        float normalizedX = Mathf.InverseLerp(collisionPD.leftBoundary, collisionPD.rightBoundary, currentX);

        // Sanfter Übergang der Frequenz
        currentCarrierFrequency = Mathf.Lerp(minCarrierFreqAtLeft, maxCarrierFreqAtRight, normalizedX);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float phaseIncrement = 1f / sampleRate;

        var getCarrier = waveformFuncs[carrierWave];
        var getModulator = waveformFuncs[modulatorWave];

        for (int i = 0; i < data.Length; i += channels)
        {
            // 1. Oszillatoren auswerten
            float modulatorVal = getModulator(modulatorPhase, modulatorFrequency);
            float carrierVal = getCarrier(carrierPhase, currentCarrierFrequency);

            // Multiplikation der zwei Wellenformen
            float combinedModulation = carrierVal * modulatorVal;

            // 2. Sample in Puffer schreiben
            float inputSample = data[i];
            ringBuffer[bufferWriteHead] = inputSample;

            // 3. Echte Phasenverschiebung (FM) für flüssiges Vibrato / FM-Sound
            float offset = combinedModulation * 200f * modulationDepth;
            float readHead = bufferWriteHead - 400f + offset;

            while (readHead < 0) readHead += ringBuffer.Length;
            while (readHead >= ringBuffer.Length) readHead -= ringBuffer.Length;

            // Lineare Interpolation (verhindert das Knistern!)
            int idxA = (int)readHead;
            int idxB = (idxA + 1) % ringBuffer.Length;
            float frac = readHead - idxA;
            float modulatedSample = Mathf.Lerp(ringBuffer[idxA], ringBuffer[idxB], frac);

            // 4. Auf Audio anwenden
            for (int channel = 0; channel < channels; channel++)
            {
                float originalSample = data[i + channel];
                data[i + channel] = Mathf.Lerp(originalSample, modulatedSample, modulationDepth);
            }

            // Pufferzeiger weiterstellen
            bufferWriteHead = (bufferWriteHead + 1) % ringBuffer.Length;

            // Phasenfortschritt OHNE doppelte Frequenz-Multiplikation
            carrierPhase += phaseIncrement;
            if (carrierPhase >= 1f) carrierPhase -= 1f;

            modulatorPhase += phaseIncrement;
            if (modulatorPhase >= 1f) modulatorPhase -= 1f;
        }
    }
}