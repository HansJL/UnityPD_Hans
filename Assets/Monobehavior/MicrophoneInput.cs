using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Captures live microphone input and routes it through the AudioSource on this
/// GameObject. When used with LibPdInstance, the mic signal reaches adc~ via the
/// looping mic AudioClip before libpd_process_float runs. With silenceOutput enabled,
/// this script clears the buffer after LibPdInstance so analysis-only patches do not
/// monitor the mic to the speakers.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(100)]
public class MicrophoneInput : MonoBehaviour
{
    [Header("Microphone Settings")]
    [Tooltip("Exact or partial device name. When empty, the first device is used.")]
    [SerializeField] private string deviceName = "Mic/Inst/Line In";

    [Tooltip("Ring buffer length in seconds passed to Microphone.Start.")]
    [SerializeField] [Min(1)] private int bufferLengthSec = 1;

    [Tooltip("When enabled, silences speaker output after LibPdInstance processes the buffer.")]
    [SerializeField] private bool silenceOutput = true;

    [SerializeField] private bool startOnAwake = true;

    private AudioSource audioSource;
    private string activeDevice;
    private bool isRecording;
    private AudioClip micClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (startOnAwake)
            StartCoroutine(StartMicrophoneRoutine());
    }

    void OnDisable()
    {
        StopMicrophone();
    }

    public void StartMicrophone()
    {
        StartCoroutine(StartMicrophoneRoutine());
    }

    public void StopMicrophone()
    {
        if (!isRecording)
            return;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (!string.IsNullOrEmpty(activeDevice))
            Microphone.End(activeDevice);

        isRecording = false;
        activeDevice = null;
    }

    private IEnumerator StartMicrophoneRoutine()
    {
        if (isRecording)
            yield break;

#if UNITY_ANDROID || UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogError($"[{nameof(MicrophoneInput)}] Microphone permission denied on {gameObject.name}.");
                yield break;
            }
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError($"[{nameof(MicrophoneInput)}] No microphone found on {gameObject.name}.");
            yield break;
        }

        activeDevice = ResolveDeviceName();
        int sampleRate = ResolveSampleRate(activeDevice);
        micClip = Microphone.Start(activeDevice, true, bufferLengthSec, sampleRate);

        if (micClip == null)
        {
            Debug.LogError($"[{nameof(MicrophoneInput)}] Microphone.Start failed on {gameObject.name}.");
            yield break;
        }

        int waitFrames = 0;
        while (Microphone.GetPosition(activeDevice) <= 0)
        {
            waitFrames++;
            if (waitFrames > 600)
            {
                Debug.LogError($"[{nameof(MicrophoneInput)}] Microphone never reported samples on {gameObject.name}.");
                yield break;
            }
            yield return null;
        }

        audioSource.clip = micClip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;

        isRecording = true;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!silenceOutput || !isRecording)
            return;

        Array.Clear(data, 0, data.Length);
    }

    private string ResolveDeviceName()
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            if (Array.IndexOf(Microphone.devices, deviceName) >= 0)
                return deviceName;

            foreach (string device in Microphone.devices)
            {
                if (device.IndexOf(deviceName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return device;
            }

            Debug.LogWarning($"[{nameof(MicrophoneInput)}] Device '{deviceName}' not found. Available: {string.Join(", ", Microphone.devices)}. Using first device.");
        }

        return Microphone.devices[0];
    }

    private int ResolveSampleRate(string device)
    {
        Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
        int preferred = AudioSettings.outputSampleRate;
        int chosen = preferred;

        if (maxFreq > 0)
        {
            if (preferred < minFreq || preferred > maxFreq)
            {
                chosen = maxFreq >= 44100 ? 44100 : maxFreq;
                if (chosen < minFreq)
                    chosen = minFreq;
            }
        }

        if (chosen != preferred)
        {
            Debug.LogWarning($"[{nameof(MicrophoneInput)}] Device '{device}' supports {minFreq}-{maxFreq} Hz. Using {chosen} Hz instead of Unity output rate {preferred} Hz.");
        }

        return chosen;
    }
}
