using UnityEngine;

public class CollisionSoundTrigger : MonoBehaviour
{
    // Wir verweisen auf unseren zentralen Audio-Manager
    private LibPdInstance pdInstance;

    // Die MIDI-Daten für den Kollisionssound in deinem großen Pd-Patch
    [SerializeField] private int midiChannel = 1;
    [SerializeField] private int midiCCNumber = 10; // z. B. CC 10 für "Aufprall"
    [SerializeField] private int velocity = 127;    // Lautstärke/Stärke des Sounds

    void Start()
    {
        // Sucht das zentrale Audio-Objekt in der Szene anhand seines Namens
        GameObject audioManager = GameObject.Find("PD_Audio_Manager");
        
        if (audioManager != null)
        {
            pdInstance = audioManager.GetComponent<LibPdInstance>();
        }
    }

    // Diese Funktion wird von Unity AUTOMATISCH aufgerufen, wenn etwas den Stein berührt
    private void OnCollisionEnter(Collision collision)
    {
        // Prüfen, ob das, was den Stein gerammt hat, der Spieler ist
        if (collision.gameObject.CompareTag("Player"))
        {
            if (pdInstance != null)
            {
                // Signal an das große Pd-Patch senden
                pdInstance.SendMidiCc(midiChannel, midiCCNumber, velocity);
                
                Debug.Log("Spieler ist gegen den Stein gelaufen! Signal an Pd gesendet.");
            }
        }
    }
}