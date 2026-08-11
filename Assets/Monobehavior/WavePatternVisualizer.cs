using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WavePatternVisualizer : MonoBehaviour
{
    [Header("Ziel 2D-Objekt")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("Punkte-Raster")]
    [Range(5, 80)] public int resolutionX = 30;
    [Range(5, 80)] public int resolutionY = 30;

    [Header("Partikel-Größe (Skalierung)")]
    [Tooltip("Jetzt stufenlos von winzig bis riesig anpassbar!")]
    [Range(0.001f, 2f)] public float particleSize = 0.1f;

    [Header("Wellen-Steuerung (Nutzt ControlFunctions)")]
    [Range(0f, 10f)] public float waveAmplitude = 1.5f;
    public float waveFrequency = 2f;
    public float waveSpeed = 1f;

    [Tooltip("0 = Sinus/Dreieck, 1 = Dreieck/Sägezahn, 2 = Sägezahn/Rechteck, 3 = Rechteck")]
    [Range(0f, 3f)] public float waveformShape = 0f;

    private ParticleSystem particleSys;
    private ParticleSystemRenderer particleRenderer;
    private ParticleSystem.Particle[] particles;
    private Vector2[] baseLocalPositions;
    private Color[] particleColors;
    private int totalParticles;
    private float timeAccumulator;

    void Start()
    {
        particleSys = GetComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();

        // Renderer-Grenzen per Code aufheben (Sicherheits-Fix)
        if (particleRenderer != null)
        {
            particleRenderer.maxParticleSize = 100f;
            particleRenderer.minParticleSize = 0f;
        }

        if (targetSpriteRenderer == null || targetSpriteRenderer.sprite == null)
        {
            Debug.LogError("Bitte weise einen SpriteRenderer mit gültigem Sprite im Inspector zu!");
            return;
        }

        InitSpriteAsParticles();
    }

    void InitSpriteAsParticles()
    {
        Sprite sprite = targetSpriteRenderer.sprite;
        Texture2D texture = sprite.texture;

        Bounds bounds = targetSpriteRenderer.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        totalParticles = resolutionX * resolutionY;
        particles = new ParticleSystem.Particle[totalParticles];
        baseLocalPositions = new Vector2[totalParticles];
        particleColors = new Color[totalParticles];

        int index = 0;

        for (int y = 0; y < resolutionY; y++)
        {
            float normY = (float)y / (resolutionY - 1);
            for (int x = 0; x < resolutionX; x++)
            {
                float normX = (float)x / (resolutionX - 1);

                baseLocalPositions[index] = new Vector2(
                    Mathf.Lerp(min.x, max.x, normX) - targetSpriteRenderer.transform.position.x,
                    Mathf.Lerp(min.y, max.y, normY) - targetSpriteRenderer.transform.position.y
                );

                Color color = texture.GetPixelBilinear(normX, normY);
                particleColors[index] = color * targetSpriteRenderer.color;

                index++;
            }
        }

        var main = particleSys.main;
        main.maxParticles = totalParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particleSys.emission;
        emission.enabled = false;

        targetSpriteRenderer.enabled = false;
    }

    void Update()
    {
        if (particles == null) return;

        timeAccumulator += Time.deltaTime * waveSpeed;
        Vector3 originPos = targetSpriteRenderer.transform.position;

        for (int i = 0; i < totalParticles; i++)
        {
            Vector2 localPos = baseLocalPositions[i];
            float distFromCenter = localPos.magnitude;

            // Mathe aus deinem ControlFunctions-Skript
            float lfoOffset = ControlFunctions.Lfo(
                distFromCenter - timeAccumulator, 
                waveFrequency, 
                waveAmplitude, 
                0f, 
                waveformShape
            );

            Vector3 direction = localPos.normalized;
            if (direction == Vector3.zero) direction = Vector3.up;

            Vector3 currentPos = originPos + (Vector3)localPos + (direction * lfoOffset);

            particles[i].position = currentPos;
            
            // WICHTIG: Lebensdauer setzen, damit Unity die Größenskalierung akzeptiert!
            particles[i].startLifetime = 99999f;
            particles[i].remainingLifetime = 99999f;
            
            // Größe zuweisen
            particles[i].startSize = particleSize;

            Color waveColor = ControlFunctions.float2Color(lfoOffset + timeAccumulator, 1f);
            particles[i].startColor = Color.Lerp(particleColors[i], waveColor, 0.3f);
        }

        particleSys.SetParticles(particles, totalParticles);
    }
}