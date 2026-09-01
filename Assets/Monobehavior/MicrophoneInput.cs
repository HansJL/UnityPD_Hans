using System.Collections;
using UnityEngine;

/// <summary>
/// Captures live microphone input and routes it through the AudioSource on this
/// GameObject. When used with LibPdInstance, the mic signal reaches adc~ the same
/// way a looping AudioClip does.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MicrophoneInput : MonoBehaviour
{
    [Header("Microphone Settings")]
    [Tooltip("Leave empty to use the system default microphone.")]
    [SerializeField] private string deviceName;

    [Tooltip("Ring buffer length in seconds passed to Microphone.Start.")]
    [SerializeField] [Min(1)] private int bufferLengthSec = 1;

    [Tooltip("Extra playback volume applied to the mic signal.")]
    [SerializeField] [Range(0f, 2f)] private float volume = 1f;

    [SerializeField] private bool startOnAwake = true;

    private AudioSource audioSource;
    private string activeDevice;
    private bool isRecording;

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
        int sampleRate = AudioSettings.outputSampleRate;
        AudioClip micClip = Microphone.Start(activeDevice, true, bufferLengthSec, sampleRate);

        if (micClip == null)
        {
            Debug.LogError($"[{nameof(MicrophoneInput)}] Microphone.Start failed on {gameObject.name}.");
            yield break;
        }

        while (Microphone.GetPosition(activeDevice) <= 0)
            yield return null;

        audioSource.clip = micClip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.mute = false;
        audioSource.volume = volume;

        if (!audioSource.isPlaying)
            audioSource.Play();

        isRecording = true;
    }

    private string ResolveDeviceName()
    {
        if (!string.IsNullOrEmpty(deviceName) && System.Array.IndexOf(Microphone.devices, deviceName) >= 0)
            return deviceName;

        if (!string.IsNullOrEmpty(deviceName))
            Debug.LogWarning($"[{nameof(MicrophoneInput)}] Device '{deviceName}' not found. Using default.");

        return Microphone.devices[0];
    }
}
