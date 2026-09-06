using UnityEngine;

public class ListMicrophones : MonoBehaviour
{
    void Start()
    {
        string[] devices = Microphone.devices;

        if (devices.Length == 0)
        {
            Debug.LogWarning("Keine Mikrofone oder Audio-Interfaces gefunden!");
            return;
        }

        Debug.Log($"[{devices.Length}] Audiodesign/Mikrofon-Geräte gefunden:");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Geraet [{i}]: \"{devices[i]}\"");
        }
    }
}