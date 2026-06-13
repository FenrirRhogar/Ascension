using UnityEngine;

// Moving this to its own file ensures that even if LevelManager fails to compile,
// the rest of the project (like ProceduralTerrain) can still see this enum.
public enum BiomeType 
{ 
    Forest, 
    Volcanic, 
    Frozen,
    Desert,
    Autumn
}
