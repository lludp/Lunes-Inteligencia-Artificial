using UnityEngine;

public class ObjectGlow : MonoBehaviour
{
    [Header("Configuración de Brillo")]
    [ColorUsage(true, true)] 
    public Color glowColor = Color.yellow;

    public float minIntensity = 0f;
    public float maxIntensity = 2.5f;
    public float pulseSpeed = 2.0f;

    private Material material;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();

        material = renderer.material;

        material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        float currentIntensity = minIntensity + Mathf.PingPong(Time.time * pulseSpeed, maxIntensity - minIntensity);

        Color finalColor = glowColor * currentIntensity;

        material.SetColor("_EmissionColor", finalColor);
    }
}