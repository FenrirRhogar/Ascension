using UnityEngine;

[CreateAssetMenu(fileName = "NewClass", menuName = "DungeonCrawler/Class")]
public class GenericClassSO : CharacterClassSO
{
    [Header("Action Settings")]
    public string actionTriggerName = "Attack";
    public GameObject effectPrefab;

    public override void ExecuteAttack(PlayerController player, Animator animator)
    {
        ExecuteAction(player, animator);
    }

    public override void ExecuteAbility(PlayerController player, Animator animator)
    {
        ExecuteAction(player, animator);
    }

    public override void ExecuteUltimate(PlayerController player, Animator animator)
    {
        ExecuteAction(player, animator);
        Debug.Log("Generic Ultimate executed!");
    }

    public override void ExecuteAction(PlayerController player, Animator animator)
    {
        if (animator != null)
        {
            animator.SetTrigger(actionTriggerName);
        }

        Debug.Log($"{className} executed action!");

        if (effectPrefab != null)
        {
            // Center visual effects using the base class offset
            Vector3 spawnPos = player.transform.position + Vector3.up * vfxHeightOffset;
            GameObject vfx = Instantiate(effectPrefab, spawnPos + player.transform.forward, player.transform.rotation);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 3f);
        }

        var rs = player.GetComponent<ResourceSystem>();
        if (rs != null) rs.AddUltimateCharge(10f);
    }
}
