using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    private LibPdInstance pdInstance;

    // Eine eigene CC-Nummer NUR für die Musik (z. B. CC 20)
    private const int musicVolumeCC = 20;
    private const int midiChannel = 1;

    void Start()
    {
        // Verbindung zum zentralen Audio-Manager herstellen
        GameObject audioManager = GameObject.Find("PD_Audio_Manager");
        if (audioManager != null)
        {
            pdInstance = audioManager.GetComponent<LibPdInstance>();
        }

        // Sobald das Spiel startet, sagen wir Pd: "Musik an!" (Lautstärke auf 100)
        StartBackgroundMusic();
    }

    public void StartBackgroundMusic()
    {
        if (pdInstance != null)
        {
            pdInstance.SendMidiCc(midiChannel, musicVolumeCC, 100);
            Debug.Log("Hintergrundmusik via CC 20 gestartet.");
        }
    }

    // Beispiel: Wenn der Spieler stirbt oder das Spiel pausiert, blenden wir die Musik aus
    public void SetMusicVolume(int volume)
    {
        int clampedVolume = Mathf.Clamp(volume, 0, 127);
        pdInstance.SendMidiCc(midiChannel, musicVolumeCC, clampedVolume);
    }
}