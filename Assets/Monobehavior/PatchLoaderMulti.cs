using UnityEngine;

public class PatchLoaderMulti : MonoBehaviour
{
    [SerializeField] private LibPdInstance patch; // Hier kommt das LibPd-Objekt rein

    [Header("MIDI Einstellungen für dieses Objekt")]
    [SerializeField] private int midiChannel = 0;   // In Unity einstellbar
    [SerializeField] private int midiCCNumber = 1;  // In Unity einstellbar

    void Update()
    {
        // Berechnet die automatische Welle
        float time = Mathf.Sin(Time.time) * 0.5f + 0.5f;
        
        if (patch != null)
        {
            // Nutzt jetzt die oben eingestellten Variablen statt fester Zahlen!
            patch.SendMidiCc(midiChannel, midiCCNumber, (int)(time * 127));
        }
    }
}