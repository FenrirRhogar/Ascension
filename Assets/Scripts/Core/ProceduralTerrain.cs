using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrain : MonoBehaviour
{
    public BiomeType currentBiome = BiomeType.Forest;
    public int xSize = 50;
    public int zSize = 50;
    public float scale = 0.5f;
    public float heightMultiplier = 5f;
    public float noiseScale = 0.1f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Color[] colors;

    public void Generate()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Procedural Terrain";

        CreateShape();
        UpdateMesh();
        
        // Ensure collider is updated and baked immediately
        var mc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;

        // Force this object to the Default layer so raycasts hit it
        gameObject.layer = 0; 
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        uvs = new Vector2[vertices.Length];
        colors = new Color[vertices.Length];
        
        bool isBossArena = LevelManager.CurrentLevel == 5;

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float height = 0;
                
                if (isBossArena)
                {
                    // Create a large, mostly flat arena with raised edges to keep the player in
                    float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(xSize / 2f, zSize / 2f));
                    if (distFromCenter > (xSize * 0.4f)) 
                    {
                        // Steep walls at the very edge to act as an arena wall
                        float t = distFromCenter - (xSize * 0.4f);
                        height = t * t * 0.5f; 
                        height = Mathf.Min(height, heightMultiplier);
                    }
                    else
                    {
                        // Slight noise in the center so it isn't perfectly flat
                        height = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 0.5f;
                    }
                }
                else
                {
                    // Multi-octave noise for smoother, natural mountains
                    float freq = noiseScale;
                    float amp = heightMultiplier;
                    float maxValue = 0;
                    
                    for (int octave = 0; octave < 3; octave++)
                    {
                        height += Mathf.PerlinNoise(x * freq, z * freq) * amp;
                        maxValue += amp;
                        freq *= 2.0f;
                        amp *= 0.5f;
                    }

                    // Normalize and apply a curve to create valleys
                    height = (height / maxValue) * heightMultiplier;
                    height = Mathf.Pow(height / heightMultiplier, 1.2f) * heightMultiplier;
                }

                vertices[i] = new Vector3(x * scale - (xSize * scale / 2f), height, z * scale - (zSize * scale / 2f));
                
                // Fix: Relaxed UV tiling (repeats every 20 meters) to avoid "random static" look
                uvs[i] = new Vector2(vertices[i].x * 0.05f, vertices[i].z * 0.05f);
                
                i++;
            }
        }

        // --- Calculate Splat Weights (R=Ground, G=Rock, B=Peak) ---
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float h = vertices[i].y;
                
                // Calculate "steepness"
                float slope = 0;
                if (x < xSize && z < zSize)
                {
                    float hRight = vertices[i + 1].y;
                    float hUp = vertices[i + xSize + 1].y;
                    slope = (Mathf.Abs(h - hRight) + Mathf.Abs(h - hUp)) / scale;
                }

                float rockWeight = Mathf.Clamp01((slope - 1.2f) / 0.8f);
                float peakWeight = Mathf.Clamp01((h - heightMultiplier * 0.7f) / (heightMultiplier * 0.2f));
                float groundWeight = 1.0f - Mathf.Max(rockWeight, peakWeight);

                colors[i] = new Color(groundWeight, rockWeight, peakWeight, 1.0f);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors; // Apply biome colors
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
