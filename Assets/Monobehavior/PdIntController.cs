using UnityEngine;

public class PdIntController : MonoBehaviour
{
    [Header("LibPd Anbindung")]
    public LibPdInstance pdInstance; 

    [Header("Deine Steuerungs-Variable")]
    [Range(0, 127)] 
    public int sampledValuePhasor = 0; 

    [Range(0, 20)]
    public float samplingNumberPhasor = 1.0f;

    [Range(0, 1)]
    [SerializeField] float volume = 0.5f;

    // Die CC-Nummer für dein [ctlin 4 1] in Pure Data
    private const int CCNumberSampling = 2; 
    private const int CCNumberValue = 1; 
    private const int midiChannel = 0; 
    private const int CCNumberVolume = 3; 

    void Update()
    {   
        pdInstance.SendMidiCc(midiChannel, CCNumberSampling, (int)samplingNumberPhasor);
        pdInstance.SendMidiCc(midiChannel, CCNumberValue, (int)sampledValuePhasor);
        pdInstance.SendMidiCc(midiChannel, CCNumberVolume, (int)(volume * 127));
    }


    

   
}