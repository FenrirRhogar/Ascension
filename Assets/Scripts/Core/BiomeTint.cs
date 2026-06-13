using UnityEngine;

public class BiomeTint : MonoBehaviour
{
    public void ApplyTint(BiomeType biome)
    {
        Color tint = Color.white;

        switch (biome)
        {
            case BiomeType.Autumn:
                // Red/Orange/Yellow tint for trees
                tint = new Color(1.0f, 0.4f, 0.0f); 
                break;
            case BiomeType.Frozen:
                // Blue/White tint for snow
                tint = new Color(0.8f, 0.9f, 1.0f);
                break;
            case BiomeType.Volcanic:
                // Dark/Charred tint
                tint = new Color(0.3f, 0.2f, 0.2f);
                break;
            case BiomeType.Desert:
                // Bleached/Sandy tint
                tint = new Color(1.0f, 0.9f, 0.7f);
                break;
            default:
                tint = Color.white;
                break;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                // Try to set the tint. Usually _BaseColor in URP
                if (m.HasProperty("_BaseColor"))
                {
                    Color original = m.GetColor("_BaseColor");
                    m.SetColor("_BaseColor", original * tint);
                }
                else if (m.HasProperty("_Color"))
                {
                    Color original = m.GetColor("_Color");
                    m.SetColor("_Color", original * tint);
                }
            }
        }
    }
}
