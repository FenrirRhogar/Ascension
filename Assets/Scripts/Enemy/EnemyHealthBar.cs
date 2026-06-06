using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 4f, 0);    
    public Vector2 size = new Vector2(3.5f, 0.4f); // Made wider and thicker
    
    private Health enemyHealth;
    
    private class PlayerUIInstance
    {
        public GameObject canvasGO;
        public Slider slider;
    }

    private Dictionary<int, PlayerUIInstance> playerUIs = new Dictionary<int, PlayerUIInstance>();

    void Start()
    {
        enemyHealth = GetComponent<Health>();
        if (enemyHealth == null) enemyHealth = GetComponentInParent<Health>();
        if (enemyHealth == null) enemyHealth = GetComponentInChildren<Health>();
    }

    public void ShowForPlayer(int playerIndex, int layer)
    {
        if (playerUIs == null) return;

        if (!playerUIs.ContainsKey(playerIndex))
        {
            CreateHealthBar(playerIndex, layer);
        }

        if (playerUIs.ContainsKey(playerIndex))
        {
            var instance = playerUIs[playerIndex];
            if (instance != null && instance.canvasGO != null)
            {
                instance.canvasGO.SetActive(true);
            }
        }
    }

    void CreateHealthBar(int playerIndex, int layer)
    {
        PlayerUIInstance instance = new PlayerUIInstance();

        // 1. Create Canvas
        instance.canvasGO = new GameObject($"HealthBar_P{playerIndex}", typeof(RectTransform));
        if (instance.canvasGO == null) return;

        instance.canvasGO.transform.SetParent(this.transform);
        instance.canvasGO.transform.localPosition = offset;
        instance.canvasGO.layer = layer;

        Canvas canvas = instance.canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = instance.canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;
        canvasRect.localScale = Vector3.one * 0.5f; 

        // 2. Create Slider Root
        GameObject sliderGO = new GameObject("Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(instance.canvasGO.transform, false);
        sliderGO.layer = layer;
        
        instance.slider = sliderGO.AddComponent<Slider>();
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        // 3. Border (White Outline)
        GameObject borderGO = new GameObject("Border", typeof(RectTransform));
        borderGO.transform.SetParent(sliderGO.transform, false);
        borderGO.layer = layer;
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = Color.white;
        RectTransform borderRect = borderGO.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero; // Match canvas size exactly

        // 3.5 Background (Black interior)
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(sliderGO.transform, false);
        bgGO.layer = layer;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); 
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(0.05f, 0.05f); // Inset by 0.05 units to show border
        bgRect.offsetMax = new Vector2(-0.05f, -0.05f);

        // 4. Fill Area
        GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        fillAreaGO.layer = layer;
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(0.05f, 0.05f); 
        fillAreaRect.offsetMax = new Vector2(-0.05f, -0.05f);

        // 5. Fill
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        fillGO.layer = layer;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.15f, 0.15f, 1f); // Vibrant red
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        instance.slider.fillRect = fillRect;
        instance.slider.targetGraphic = fillImg;
        instance.slider.minValue = 0;
        instance.slider.maxValue = (enemyHealth != null) ? enemyHealth.MaxHealth : 100;
        instance.slider.value = instance.slider.maxValue;
        instance.slider.interactable = false;

        if (playerUIs != null) playerUIs[playerIndex] = instance;
    }

    void Update()
    {
        if (enemyHealth == null || playerUIs == null) return;
    
        foreach (var kvp in playerUIs)
        {
            var instance = kvp.Value;
            if (instance == null || instance.canvasGO == null || instance.slider == null) continue;
            
            // Sync value
            instance.slider.value = enemyHealth.currentHealth;

            // Face player camera
            var players = UnityEngine.InputSystem.PlayerInput.all;
            if (kvp.Key < players.Count && players[kvp.Key] != null)
            {
                Camera playerCam = players[kvp.Key].GetComponentInChildren<Camera>();
                if (playerCam != null)
                {
                    instance.canvasGO.transform.LookAt(instance.canvasGO.transform.position + playerCam.transform.rotation * Vector3.forward, playerCam.transform.rotation * Vector3.up);
                }
            }
            
            if (!instance.canvasGO.activeSelf) instance.canvasGO.SetActive(true);
        }
    }
}
