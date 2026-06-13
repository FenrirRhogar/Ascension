using UnityEngine;

public class MenuVisuals : MonoBehaviour
{
    [Header("Atmosphere")]
    public BiomeType menuBiome = BiomeType.Forest;
    public bool forceNight = false;

    [Header("Sky Movement")]
    public float skyRotationSpeed = 0.5f;

    void Start()
    {
        // Apply the cool atmosphere settings we built earlier
        if (AtmosphereManager.Instance != null)
        {
            AtmosphereManager.Instance.ApplyAtmosphere(menuBiome, forceNight);
        }
        else
        {
            // Auto-create if it's missing in the menu scene
            gameObject.AddComponent<AtmosphereManager>().ApplyAtmosphere(menuBiome, forceNight);
        }
    }

    void Update()
    {
        // Slowly rotate the skybox to make the environment feel "alive"
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Rotation"))
        {
            float currentRotation = RenderSettings.skybox.GetFloat("_Rotation");
            RenderSettings.skybox.SetFloat("_Rotation", currentRotation + skyRotationSpeed * Time.deltaTime);
        }
    }
}
