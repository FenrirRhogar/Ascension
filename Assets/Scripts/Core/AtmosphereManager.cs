using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    public static AtmosphereManager Instance { get; private set; }

    [Header("Global Lighting")]
    public Light directionalLight;
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.2f;

    [Header("Fog Settings (Linear)")]
    public bool enableFog = true;
    public float fogStartDay = 10f;
    public float fogEndDay = 150f;
    public float fogStartNight = 5f;
    public float fogEndNight = 60f;

    [Header("Skybox Materials")]
    public Material forestSky;
    public Material forestNightSky;
    public Material volcanicSky;
    public Material frozenSky;
    public Material frozenNightSky;
    public Material desertSky;
    public Material autumnSky;

    public bool isNight { get; private set; }

    void Awake()
    {
        Instance = this;
        // Ensure we are using Linear fog for predictable results
        RenderSettings.fogMode = FogMode.Linear;
        
        if (directionalLight == null)
        {
            // Try to find the Sun
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }
        }
    }

    public void ApplyAtmosphere(BiomeType biome, bool forceNight = false)
    {
        // 30% chance of night, or forced for specific levels
        isNight = forceNight || (Random.value < 0.3f);

        // Default neutral colors (Forest)
        Color lightColor = new Color(1f, 1f, 0.9f); 
        Color skyColor = new Color(0.5f, 0.6f, 0.7f); 
        Color fogColor = new Color(0.6f, 0.65f, 0.7f);
        Material selectedSky = forestSky;

        switch (biome)
        {
            case BiomeType.Forest:
                lightColor = new Color(1f, 1f, 0.95f);
                skyColor = new Color(0.45f, 0.55f, 0.65f);
                fogColor = new Color(0.55f, 0.6f, 0.65f);
                selectedSky = isNight ? forestNightSky : forestSky;
                break;
            case BiomeType.Volcanic:
                lightColor = new Color(1f, 0.6f, 0.4f);
                skyColor = new Color(0.4f, 0.2f, 0.15f);
                fogColor = new Color(0.3f, 0.15f, 0.1f);
                selectedSky = volcanicSky;
                break;
            case BiomeType.Frozen:
                lightColor = new Color(0.85f, 0.95f, 1f);
                skyColor = new Color(0.6f, 0.7f, 0.85f);
                fogColor = new Color(0.8f, 0.85f, 0.9f);
                selectedSky = isNight ? frozenNightSky : frozenSky;
                break;
            case BiomeType.Autumn:
                lightColor = new Color(1f, 0.8f, 0.5f);
                skyColor = new Color(0.6f, 0.45f, 0.3f);
                fogColor = new Color(0.5f, 0.35f, 0.25f);
                selectedSky = autumnSky;
                break;
            case BiomeType.Desert:
                lightColor = new Color(1f, 0.9f, 0.7f);
                skyColor = new Color(0.85f, 0.75f, 0.6f);
                fogColor = new Color(0.9f, 0.85f, 0.75f);
                selectedSky = desertSky;
                break;
        }

        if (isNight)
        {
            // Night lighting: Dark desaturated blue-grey
            lightColor = new Color(0.15f, 0.2f, 0.35f); 
            RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.08f);
            
            // Fog at night should be darker to match the ground
            fogColor = fogColor * 0.3f; 
            
            RenderSettings.fogStartDistance = fogStartNight;
            RenderSettings.fogEndDistance = fogEndNight;
            if (directionalLight != null) directionalLight.intensity = nightIntensity;
        }
        else
        {
            RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.25f);
            RenderSettings.fogStartDistance = fogStartDay;
            RenderSettings.fogEndDistance = fogEndDay;
            if (directionalLight != null) directionalLight.intensity = dayIntensity;
        }

        if (directionalLight != null) directionalLight.color = lightColor;
        
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;

        // --- Apply Selected Skybox ---
        if (selectedSky != null)
        {
            RenderSettings.skybox = selectedSky;
        }

        // --- Skybox Support for Multiple Shaders ---
        Material skyMat = RenderSettings.skybox;
        if (skyMat != null)
        {
            // Try common skybox property names
            if (skyMat.HasProperty("_Tint")) skyMat.SetColor("_Tint", skyColor);
            if (skyMat.HasProperty("_SkyGradientTop")) skyMat.SetColor("_SkyGradientTop", skyColor);
            if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", fogColor);
            if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", isNight ? 0.3f : 1.0f);
        }

        Debug.Log($"[Atmosphere] Applied {biome} (Night: {isNight}) Fog: {RenderSettings.fogEndDistance}m Sky: {(selectedSky != null ? selectedSky.name : "Default")}");
    }
}
