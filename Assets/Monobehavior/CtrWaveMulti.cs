using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CtrWaveMulti : MonoBehaviour
{
    public enum WaveDirection 
    { 
        Radial,         
        Horizontal,     
        Vertical,       
        Diagonal        
    }

    public enum ColorGlitchMode
    {
        StandardLerp,   
        RGBSplit,       
        InvertGlitch,   
        NeonPalette,    
        BitCrushedColor 
    }

    // Struct für die Wellen-Logik (wie gehabt)
    private struct WaveLogic
    {
        public Func<Vector2, float> CalculatePosition;
        public Func<Vector2, Vector3> CalculateDirection;
    }

    // Struct für die Farb-Logik (NEU)
    private struct ColorGlitchContext
    {
        public Color baseColor;
        public Color genColor;
        public float lfoOffset;
        public float time;
        public Vector2 localPos;
        public bool isGlitchedLine;
    }

    [Header("Ziel 2D-Objekt")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("Wellen-Richtung")]
    public WaveDirection waveDirection = WaveDirection.Horizontal; 

    [Header("Farb-Steuerung & Glitch")]
    public ColorGlitchMode colorMode = ColorGlitchMode.RGBSplit;
    [Range(0f, 1f)] public float colorGlitchIntensity = 0.5f; 
    [Range(0f, 1f)] public float rgbSplitIntensity = 0.8f;    
    public float colorChangeSpeed = 3f;                        

    [Header("Punkte-Raster")]
    [Range(5, 80)] public int resolutionX = 30;
    [Range(5, 80)] public int resolutionY = 30;

    [Header("Partikel-Größe")]
    [Range(0.001f, 2f)] public float particleSize = 0.1f;

    [Header("Wellen-Steuerung (Nutzt ControlFunctions)")]
    [Range(0f, 10f)] public float waveAmplitude = 1.5f;
    public float waveFrequency = 2f;
    public float waveSpeed = 1f;

    [Tooltip("0 = Sinus/Dreieck, 1 = Dreieck/Sägezahn, 2 = Sägezahn/Rechteck, 3 = Rechteck")]
    [Range(0f, 3f)] public float waveformShape = 3f;

    [Header("Digital Glitch / Noise")]
    [Range(0f, 1f)] public float glitchAmount = 0.4f;   
    [Range(0f, 20f)] public float digitalQuantize = 8f;  
    public float noiseSpeed = 5f;                        

    private ParticleSystem particleSys;
    private ParticleSystemRenderer particleRenderer;
    private ParticleSystem.Particle[] particles;
    private Vector2[] baseLocalPositions;
    private Color[] particleColors;
    private int totalParticles;
    private float timeAccumulator;

    // Dictionaries für modular benutzbare Logik
    private Dictionary<WaveDirection, WaveLogic> waveDictionary;
    private Dictionary<ColorGlitchMode, Func<ColorGlitchContext, Color>> colorDictionary;

    void Awake()
    {
        InitializeWaveDictionary();
        InitializeColorDictionary();
    }

    void InitializeWaveDictionary()
    {
        waveDictionary = new Dictionary<WaveDirection, WaveLogic>
        {
            {
                WaveDirection.Radial, new WaveLogic
                {
                    CalculatePosition = pos => pos.magnitude,
                    CalculateDirection = pos => {
                        Vector3 dir = pos.normalized;
                        return dir == Vector3.zero ? Vector3.up : dir;
                    }
                }
            },
            {
                WaveDirection.Horizontal, new WaveLogic
                {
                    CalculatePosition = pos => pos.y,
                    CalculateDirection = pos => Vector3.up
                }
            },
            {
                WaveDirection.Vertical, new WaveLogic
                {
                    CalculatePosition = pos => pos.x,
                    CalculateDirection = pos => Vector3.right
                }
            },
            {
                WaveDirection.Diagonal, new WaveLogic
                {
                    CalculatePosition = pos => pos.x + pos.y,
                    CalculateDirection = pos => new Vector3(1f, 1f, 0f).normalized
                }
            }
        };
    }

    // ==========================================
    // NEU: COLOR DICTIONARY INITIALISIERUNG
    // ==========================================
    void InitializeColorDictionary()
    {
        colorDictionary = new Dictionary<ColorGlitchMode, Func<ColorGlitchContext, Color>>
        {
            {
                ColorGlitchMode.StandardLerp, ctx => 
                    Color.Lerp(ctx.baseColor, ctx.genColor, colorGlitchIntensity)
            },
            {
                ColorGlitchMode.RGBSplit, ctx => 
                {
                    float shift = ctx.lfoOffset * rgbSplitIntensity;
                    return new Color(
                        Mathf.Clamp01(ctx.baseColor.r + shift),
                        Mathf.Clamp01(ctx.baseColor.g - shift * 0.5f),
                        Mathf.Clamp01(ctx.genColor.b + shift),
                        ctx.baseColor.a
                    );
                }
            },
            {
                ColorGlitchMode.InvertGlitch, ctx => 
                {
                    if (ctx.isGlitchedLine || Mathf.Abs(ctx.lfoOffset) > waveAmplitude * 0.5f)
                    {
                        return new Color(1f - ctx.baseColor.r, 1f - ctx.baseColor.g, 1f - ctx.baseColor.b, ctx.baseColor.a);
                    }
                    return Color.Lerp(ctx.baseColor, ctx.genColor, colorGlitchIntensity * 0.3f);
                }
            },
            {
                ColorGlitchMode.NeonPalette, ctx => 
                {
                    float hue = (ctx.localPos.y + ctx.time * colorChangeSpeed) % 1f;
                    Color neon = Color.HSVToRGB(Mathf.Abs(hue), 1f, 1f);
                    return Color.Lerp(ctx.baseColor, neon, colorGlitchIntensity);
                }
            },
            {
                ColorGlitchMode.BitCrushedColor, ctx => 
                {
                    float steps = 4f;
                    return new Color(
                        Mathf.Floor(ctx.genColor.r * steps) / steps,
                        Mathf.Floor(ctx.genColor.g * steps) / steps,
                        Mathf.Floor(ctx.genColor.b * steps) / steps,
                        ctx.baseColor.a
                    );
                }
            }
        };
    }

    void Start()
    {
        particleSys = GetComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();

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

        // Abrufen der Dictionary-Logiken für diesen Frame
        WaveLogic currentLogic = waveDictionary[waveDirection];
        Func<ColorGlitchContext, Color> currentColorLogic = colorDictionary[colorMode];

        for (int i = 0; i < totalParticles; i++)
        {
            Vector2 localPos = baseLocalPositions[i];

            float wavePosition = currentLogic.CalculatePosition(localPos);
            Vector3 moveDirection = currentLogic.CalculateDirection(localPos);

            // Stutter / Bitcrush
            float t = timeAccumulator;
            if (digitalQuantize > 0)
            {
                t = Mathf.Floor(t * digitalQuantize) / digitalQuantize;
                wavePosition = Mathf.Floor(wavePosition * digitalQuantize) / digitalQuantize;
            }

            // LFO Wellenberechnung
            float lfoOffset = ControlFunctions.Lfo(
                Mathf.Abs(wavePosition - t),
                waveFrequency,
                waveAmplitude,
                0f,
                waveformShape
            );

            // Perlin Noise
            float noise = Mathf.PerlinNoise(localPos.x * 2f, localPos.y * 2f + timeAccumulator * noiseSpeed) - 0.5f;
            lfoOffset += noise * waveAmplitude * 0.5f;

            // Spontane Zeilen-Glitches
            bool isGlitchedLine = false;
            float lineGlitchTrigger = Mathf.PerlinNoise(localPos.y * 10f, timeAccumulator * 8f);
            if (lineGlitchTrigger > (1f - glitchAmount * 0.5f))
            {
                lfoOffset += (lineGlitchTrigger - 0.5f) * waveAmplitude * 3f;
                isGlitchedLine = true;
            }

            // Position setzen
            Vector3 currentPos = originPos + (Vector3)localPos + (moveDirection * lfoOffset);
            particles[i].position = currentPos;
            particles[i].startLifetime = 99999f;
            particles[i].remainingLifetime = 99999f;
            particles[i].startSize = particleSize;

            // ==========================================
            // SAUBERER DICTIONARY-AUFRUF FÜR FARBEN
            // ==========================================
            Color baseColor = particleColors[i];
            Color genColor = ControlFunctions.float2Color(lfoOffset * 0.5f + timeAccumulator * colorChangeSpeed, 1f);

            // Erstelle Kontext mit allen Daten für die Funktion
            ColorGlitchContext context = new ColorGlitchContext
            {
                baseColor = baseColor,
                genColor = genColor,
                lfoOffset = lfoOffset,
                time = timeAccumulator,
                localPos = localPos,
                isGlitchedLine = isGlitchedLine
            };

            // Hole die Farbe direkt aus dem Dictionary
            Color finalColor = currentColorLogic(context);

            // Weißes Aufblitzen bei Glitch-Zeilen
            if (isGlitchedLine && colorGlitchIntensity > 0f)
            {
                finalColor = Color.Lerp(finalColor, Color.white, 0.6f);
            }

            particles[i].startColor = finalColor;
        }

        particleSys.SetParticles(particles, totalParticles);
    }
}