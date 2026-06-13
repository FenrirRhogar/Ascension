using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light lightSource;
    private float baseIntensity;

    [SerializeField] private float minIntensityMultiplier = 0.8f;
    [SerializeField] private float maxIntensityMultiplier = 1.2f;
    [SerializeField] private float flickerSpeed = 0.1f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (lightSource != null) baseIntensity = lightSource.intensity;
    }

    void Update()
    {
        if (lightSource == null) return;
        
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        float multiplier = Mathf.Lerp(minIntensityMultiplier, maxIntensityMultiplier, noise);
        lightSource.intensity = baseIntensity * multiplier;
    }
}
