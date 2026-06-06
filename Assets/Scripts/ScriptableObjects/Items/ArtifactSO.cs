using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewArtifact", menuName = "DungeonCrawler/Artifacts/Artifact")]
public class ArtifactSO : ConsumableSO
{
    [Header("Artifact Buffs")]
    public float speedBonus = 0f;
    public int healthBonus = 0;
    public float damageMultiplierBonus = 0f;
    public float manaRegenBonus = 0f;
    public float staminaRegenBonus = 0f;
    public float meleeRangeBonus = 0f;

    private void OnEnable()
    {
        activateOnPickup = true;
        buffDuration = 0f; // Permanent run-wide effects
    }

    public override void UseItem(PlayerController player)
    {
        ArtifactManager am = player.GetComponent<ArtifactManager>();
        if (am == null) am = player.gameObject.AddComponent<ArtifactManager>();
        am.AddArtifact(this);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArtifactSO))]
[CanEditMultipleObjects]
public class ArtifactSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Draw all fields except the base fields we want to hide for artifacts
        DrawPropertiesExcluding(serializedObject, "buffDuration", "activateOnPickup", "m_Script");
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
