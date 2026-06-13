using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 3.5f, 0);    
    public Vector2 size = new Vector2(2.5f, 0.25f); // Smaller and thinner
    public float hideDelay = 4f; // Hide after 4 seconds of no activity
    
    private Health enemyHealth;
    private int lastHealth;
    private float lastUpdateTime;
    private Dictionary<int, Slider> playerSliders = new Dictionary<int, Slider>();

    void Start()
    {
        enemyHealth = GetComponent<Health>();
        if (enemyHealth == null) enemyHealth = GetComponentInParent<Health>();
        if (enemyHealth != null) lastHealth = enemyHealth.currentHealth;
    }

    public void ShowForPlayer(int playerIndex, int layer)
    {
        lastUpdateTime = Time.time;
        if (playerSliders.ContainsKey(playerIndex)) 
        {
            if (!playerSliders[playerIndex].gameObject.activeSelf)
                playerSliders[playerIndex].gameObject.SetActive(true);
            return;
        }

        CreateHealthBar(playerIndex, layer);
    }

    void CreateHealthBar(int playerIndex, int layer)
    {
        // 1. Create Root Canvas
        GameObject canvasGO = new GameObject($"HealthBar_P{playerIndex}", typeof(RectTransform));
        canvasGO.transform.SetParent(this.transform);
        canvasGO.transform.localPosition = offset;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.015f; 
        
        canvasGO.layer = 0; 

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(size.x * 50, size.y * 50);

        // 2. Background
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 3. Fill Area
        GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-2, -2); 

        // 4. Fill
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;

        // 5. Slider
        Slider slider = canvasGO.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.minValue = 0;
        slider.maxValue = (enemyHealth != null) ? enemyHealth.MaxHealth : 100;
        slider.value = slider.maxValue;
        slider.interactable = false;

        playerSliders[playerIndex] = slider;
        
        foreach(Transform t in canvasGO.GetComponentsInChildren<Transform>()) t.gameObject.layer = canvasGO.layer;
    }

    void Update()
    {
        if (enemyHealth == null) return;

        // Auto-show if health changed
        if (enemyHealth.currentHealth != lastHealth)
        {
            lastHealth = enemyHealth.currentHealth;
            lastUpdateTime = Time.time;
            foreach(var s in playerSliders.Values) 
                if(!s.gameObject.activeSelf) s.gameObject.SetActive(true);
        }

        bool shouldHide = (Time.time - lastUpdateTime) > hideDelay;
    
        foreach (var kvp in playerSliders)
        {
            Slider slider = kvp.Value;
            if (slider == null) continue;
            
            if (shouldHide) 
            {
                if (slider.gameObject.activeSelf) slider.gameObject.SetActive(false);
                continue;
            }

            slider.value = enemyHealth.currentHealth;

            var players = UnityEngine.InputSystem.PlayerInput.all;
            foreach(var p in players)
            {
                if(p.playerIndex == kvp.Key)
                {
                    Camera cam = p.GetComponentInChildren<Camera>();
                    if(cam != null)
                    {
                        slider.transform.LookAt(slider.transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
                    }
                    break;
                }
            }
        }
    }
}
