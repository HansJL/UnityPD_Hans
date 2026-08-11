using UnityEngine;

public class SpeedController : MonoBehaviour
{
    public enum GeneratorType
    {
        LFO,
        PerlinNoise,
        RandomValue
    }

    [Header("Ziel-Objekt")]
    [SerializeField] private CollisionPD targetCollisionScript;

    [Header("Generator Einstellungen")]
    [SerializeField] private GeneratorType currentType = GeneratorType.PerlinNoise;

    [Header("Allgemeine Tempo-Limits")]
    [Range(0f, 3f)] [SerializeField] private float minSpeed = 0f;
    [Range(0.1f, 5f)] [SerializeField] private float maxSpeed = 2f;

    [Header("Pausen-Einstellungen")]
    [Tooltip("Aktiviert automatische Pausen, wenn der generierte Wert unter die Schwelle fällt.")]
    [SerializeField] private bool enablePauses = true;
    [Range(0f, 1f)]
    [Tooltip("Werte unter diesem Schwellenwert setzen den Speed direkt auf 0.")]
    [SerializeField] private float pauseThreshold = 0.2f;

    [Header("1. LFO Einstellungen (Verwendet ControlFunctions)")]
    [SerializeField] private float lfoFrequency = 0.5f;
    [SerializeField] private float lfoPhase = 0f;
    [Range(0f, 3f)]
    [Tooltip("0 = Sin->Tri, 1 = Tri->Saw, 2 = Saw->Squ, 3 = Pure Squ")]
    [SerializeField] private float lfoShape = 0f;

    [Header("2. Noise Einstellungen")]
    [SerializeField] private float noiseFrequency = 0.5f;

    [Header("3. Random Einstellungen")]
    [Tooltip("Wie oft pro Sekunde wird ein neuer Zufallswert gewählt?")]
    [SerializeField] private float randomInterval = 1f;

    // Interne Timer & Werte
    private float timer = 0f;
    private float randomTimer = 0f;
    private float currentRandomValue = 0.5f;
    private float targetRandomValue = 0.5f;
    private float noiseSeed;

    void Start()
    {
        // Falls kein Script im Inspector zugewiesen wurde, suche auf demselben Objekt
        if (targetCollisionScript == null)
        {
            targetCollisionScript = GetComponent<CollisionPD>();
        }

        noiseSeed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (targetCollisionScript == null) return;

        timer += Time.deltaTime;
        float rawValue = 0f; // Erwarteter Wert im Bereich [0..1]

        switch (currentType)
        {
            case GeneratorType.LFO:
                // Nutzt deine ControlFunctions.LfoU für unipolare Werte (0 bis 1)
                rawValue = ControlFunctions.LfoU(timer, lfoFrequency, 1f, lfoPhase, lfoShape);
                break;

            case GeneratorType.PerlinNoise:
                // Sanftes, unregelmäßiges Rauschen
                rawValue = Mathf.PerlinNoise(noiseSeed, timer * noiseFrequency);
                break;

            case GeneratorType.RandomValue:
                // Wechselt in festen Intervallen zu neuen Zielwerten
                randomTimer += Time.deltaTime;
                if (randomTimer >= randomInterval)
                {
                    randomTimer = 0f;
                    targetRandomValue = Random.value; // Zufallswert 0..1
                }
                // Sanftes Interpolieren zum neuen Zufallswert
                currentRandomValue = Mathf.Lerp(currentRandomValue, targetRandomValue, Time.deltaTime * 5f);
                rawValue = currentRandomValue;
                break;
        }

        // Check für Pausen
        float calculatedSpeed = 0f;
        if (enablePauses && rawValue < pauseThreshold)
        {
            calculatedSpeed = 0f; // Pause!
        }
        else
        {
            // Wenn Pausen aktiv sind, normieren wir den verbleibenden Bereich wieder auf 0..1
            float normalizedValue = enablePauses 
                ? Mathf.InverseLerp(pauseThreshold, 1f, rawValue) 
                : rawValue;

            calculatedSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedValue);
        }

        // An das Hauptskript übergeben
        targetCollisionScript.SetGlobalSpeed(calculatedSpeed);
    }
}