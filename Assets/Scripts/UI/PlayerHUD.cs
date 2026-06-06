using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    private PlayerController player;
    private Health health;
    private ResourceSystem resources;
    private InventoryManager inventory;
    private BuffManager buffManager;
    
    private Slider healthSlider;
    private TextMeshProUGUI healthText;
    private Slider manaSlider;
    private TextMeshProUGUI manaText;
    private Slider staminaSlider;
    private TextMeshProUGUI staminaText;
    private Slider ultimateSlider;
    private TextMeshProUGUI ultimateText;
    private Image ultimateFill;

    private Image[] slotIcons = new Image[3];
    private Image[] slotCooldowns = new Image[3];
    private bool isSetup = false;

    // Tooltip UI
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;
    private Camera myCam;

    // Stats UI (Top Left)
    private TextMeshProUGUI statsText;

    // Buff UI (Top Center)
    private GameObject buffPanel;
    private Slider speedBuffBar;
    private Slider staminaBuffBar;
    private Slider healthRegenBuffBar;
    private Slider manaRegenBuffBar;
    private Slider staminaRegenBuffBar;
    private Slider infiniteManaBuffBar;

    // Threat UI (Top Center)
    private TextMeshProUGUI threatText;

    // Artifact UI
    private GameObject artifactPanel;
    private System.Collections.Generic.List<GameObject> artifactIconGOs = new System.Collections.Generic.List<GameObject>();
    private int lastArtifactsCount = -1;

    public void Setup(PlayerController p, Health h, ResourceSystem r, Camera cam)
    {
        if (p == null || cam == null) 
        {
            Debug.LogError("[PlayerHUD] Cannot setup HUD without a player and camera!");
            return;
        }

        player = p;
        health = h;
        resources = r;
        myCam = cam;
        inventory = p.GetComponent<InventoryManager>();
        buffManager = p.GetComponent<BuffManager>();

        // 1. Create Canvas
        GameObject canvasGO = new GameObject("PlayerHUD_Canvas", typeof(RectTransform));
        canvasGO.transform.SetParent(this.transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Resource Panel (Bottom Left)
        GameObject resPanel = new GameObject("ResourcePanel", typeof(RectTransform));
        resPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform resRect = resPanel.GetComponent<RectTransform>();
        resRect.anchorMin = Vector2.zero; resRect.anchorMax = Vector2.zero;
        resRect.pivot = Vector2.zero; resRect.anchoredPosition = new Vector2(20, 20);
        resRect.sizeDelta = new Vector2(250, 140);

        healthSlider = CreateBar(resPanel.transform, "HealthBar", Color.green, 0, out healthText, out _);
        manaSlider = CreateBar(resPanel.transform, "ManaBar", Color.cyan, -30, out manaText, out _);
        staminaSlider = CreateBar(resPanel.transform, "StaminaBar", Color.yellow, -60, out staminaText, out _);
        ultimateSlider = CreateBar(resPanel.transform, "UltimateBar", new Color(1f, 0.5f, 0f), -90, out ultimateText, out ultimateFill);

        // 3. Inventory Panel (Bottom Right)
        GameObject invPanel = new GameObject("InventoryPanel", typeof(RectTransform));
        invPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform invRect = invPanel.GetComponent<RectTransform>();
        invRect.anchorMin = new Vector2(1, 0); invRect.anchorMax = new Vector2(1, 0);
        invRect.pivot = new Vector2(1, 0); invRect.anchoredPosition = new Vector2(-20, 20);
        invRect.sizeDelta = new Vector2(180, 60);

        for (int i = 0; i < 3; i++)
        {
            slotIcons[i] = CreateSlotUI(invPanel.transform, i);
        }

        // 4. Create Tooltip (Center Screen)
        tooltipPanel = new GameObject("TooltipPanel", typeof(RectTransform));
        tooltipPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform ttRect = tooltipPanel.GetComponent<RectTransform>();
        ttRect.anchorMin = new Vector2(0.5f, 0.5f);
        ttRect.anchorMax = new Vector2(0.5f, 0.5f);
        ttRect.pivot = new Vector2(0.5f, 0.5f);
        ttRect.anchoredPosition = new Vector2(0, -40);
        ttRect.sizeDelta = new Vector2(200, 40);

        Image ttBg = tooltipPanel.AddComponent<Image>();
        ttBg.color = new Color(0, 0, 0, 0.7f);

        GameObject ttTextGO = new GameObject("TooltipText", typeof(RectTransform));
        ttTextGO.transform.SetParent(tooltipPanel.transform, false);
        tooltipText = ttTextGO.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 16;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.color = Color.white;
        RectTransform textRect = ttTextGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        tooltipPanel.SetActive(false);

        // 5. Create Stats Panel (Top Left)
        GameObject statsPanel = new GameObject("StatsPanel", typeof(RectTransform));
        statsPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform statsRect = statsPanel.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 1);
        statsRect.anchorMax = new Vector2(0, 1);
        statsRect.pivot = new Vector2(0, 1);
        statsRect.anchoredPosition = new Vector2(20, -20);
        statsRect.sizeDelta = new Vector2(300, 100);

        statsText = statsPanel.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 18;
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.color = Color.white;
        statsText.fontStyle = FontStyles.Bold;
        statsText.lineSpacing = 10f;

        // 6. Create Threat Panel (Top Center)
        GameObject threatPanel = new GameObject("ThreatPanel", typeof(RectTransform));
        threatPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform tRect = threatPanel.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1); tRect.anchorMax = new Vector2(0.5f, 1);
        tRect.pivot = new Vector2(0.5f, 1); tRect.anchoredPosition = new Vector2(0, -10);
        tRect.sizeDelta = new Vector2(260, 36);

        Image threatBg = threatPanel.AddComponent<Image>();
        threatBg.color = new Color(0, 0, 0, 0.6f);

        GameObject threatTextGO = new GameObject("ThreatText", typeof(RectTransform));
        threatTextGO.transform.SetParent(threatPanel.transform, false);
        threatText = threatTextGO.AddComponent<TextMeshProUGUI>();
        threatText.fontSize = 15;
        threatText.alignment = TextAlignmentOptions.Center;
        threatText.color = Color.white;
        threatText.fontStyle = FontStyles.Bold;
        RectTransform ttRect2 = threatTextGO.GetComponent<RectTransform>();
        ttRect2.anchorMin = Vector2.zero; ttRect2.anchorMax = Vector2.one;
        ttRect2.sizeDelta = Vector2.zero;

        // 7. Create Buff Panel (Top Center, shifted down below Threat Panel)
        buffPanel = new GameObject("BuffPanel", typeof(RectTransform));
        buffPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform bRect = buffPanel.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 1); bRect.anchorMax = new Vector2(0.5f, 1);
        bRect.pivot = new Vector2(0.5f, 1); bRect.anchoredPosition = new Vector2(0, -55);
        bRect.sizeDelta = new Vector2(200, 160);

        // 8. Create Artifact Panel (Bottom Right, placed above the Potion Slots)
        artifactPanel = new GameObject("ArtifactPanel", typeof(RectTransform));
        artifactPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform artifactRect = artifactPanel.GetComponent<RectTransform>();
        artifactRect.anchorMin = new Vector2(1, 0); artifactRect.anchorMax = new Vector2(1, 0);
        artifactRect.pivot = new Vector2(1, 0); artifactRect.anchoredPosition = new Vector2(-20, 90); // Positioned above Potion slots (y=20 to y=80)
        artifactRect.sizeDelta = new Vector2(250, 50);

        var layout = artifactPanel.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        speedBuffBar = CreateMiniBar(buffPanel.transform, "SpeedBuff", Color.green, 0, "SPEED");
        staminaBuffBar = CreateMiniBar(buffPanel.transform, "StaminaBuff", Color.yellow, -25, "VIGOR");
        infiniteManaBuffBar = CreateMiniBar(buffPanel.transform, "InfiniteManaBuff", new Color(0.5f, 0f, 1f), -50, "INF MANA");
        healthRegenBuffBar = CreateMiniBar(buffPanel.transform, "HealthRegenBuff", new Color(1f, 0.2f, 0.2f), -75, "REGEN HP");
        manaRegenBuffBar = CreateMiniBar(buffPanel.transform, "ManaRegenBuff", new Color(0f, 0.8f, 1f), -100, "REGEN MP");
        staminaRegenBuffBar = CreateMiniBar(buffPanel.transform, "StaminaRegenBuff", new Color(1f, 0.7f, 0f), -125, "REGEN SP");

        isSetup = true;
    }

    private Slider CreateBar(Transform parent, string name, Color color, float yOffset, out TextMeshProUGUI valueText, out Image fillImg)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform));
        sliderGO.transform.SetParent(parent, false);
        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(0, 22);

        Slider slider = sliderGO.AddComponent<Slider>();
        
        GameObject bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(sliderGO.transform, false);
        bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        fillImg = fill.AddComponent<Image>(); fillImg.color = color;
        RectTransform fRect = fill.GetComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.sizeDelta = Vector2.zero;

        slider.fillRect = fRect; 
        slider.minValue = 0; slider.maxValue = 100;
        slider.value = 100; slider.interactable = false;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(sliderGO.transform, false);
        valueText = textGO.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 14; valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = Color.white; valueText.fontStyle = FontStyles.Bold;
        RectTransform tRect = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;

        return slider;
    }

    private Slider CreateMiniBar(Transform parent, string name, Color color, float yOffset, string label)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform));
        sliderGO.transform.SetParent(parent, false);
        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(0, 15);

        Slider s = sliderGO.AddComponent<Slider>();
        
        GameObject bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(sliderGO.transform, false);
        bg.AddComponent<Image>().color = new Color(0,0,0,0.5f);
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero; bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        Image fImg = fill.AddComponent<Image>(); fImg.color = color;
        RectTransform fRect = fill.GetComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.sizeDelta = Vector2.zero;

        s.fillRect = fRect; s.minValue = 0; s.maxValue = 1; s.interactable = false;

        GameObject txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(sliderGO.transform, false);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 10; t.alignment = TextAlignmentOptions.Center;
        t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;
        t.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        sliderGO.SetActive(false);
        return s;
    }

    private Image CreateSlotUI(Transform parent, int index)
    {
        GameObject slotGO = new GameObject($"Slot_{index}", typeof(RectTransform));
        slotGO.transform.SetParent(parent, false);
        RectTransform rect = slotGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(index * 60, 0);
        rect.sizeDelta = new Vector2(50, 50);

        slotGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(slotGO.transform, false);
        Image icon = iconGO.AddComponent<Image>();
        icon.color = new Color(1, 1, 1, 0);
        RectTransform iRect = iconGO.GetComponent<RectTransform>();
        iRect.anchorMin = Vector2.one * 0.1f; iRect.anchorMax = Vector2.one * 0.9f;
        iRect.sizeDelta = Vector2.zero;

        // Cooldown Overlay
        GameObject cdGO = new GameObject("Cooldown", typeof(RectTransform));
        cdGO.transform.SetParent(slotGO.transform, false);
        Image cdImg = cdGO.AddComponent<Image>();
        cdImg.color = new Color(0, 0, 0, 0.7f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Vertical;
        cdImg.fillAmount = 0;
        RectTransform cdRect = cdGO.GetComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero; cdRect.anchorMax = Vector2.one;
        cdRect.sizeDelta = Vector2.zero;
        slotCooldowns[index] = cdImg;

        GameObject hintGO = new GameObject("Hint", typeof(RectTransform));
        hintGO.transform.SetParent(slotGO.transform, false);
        var hint = hintGO.AddComponent<TextMeshProUGUI>();
        hint.text = (index + 1).ToString(); hint.fontSize = 12;
        hint.alignment = TextAlignmentOptions.BottomRight;
        RectTransform hRect = hintGO.GetComponent<RectTransform>();
        hRect.anchorMin = Vector2.zero; hRect.anchorMax = Vector2.one;
        hRect.sizeDelta = new Vector2(-2, -2);

        return icon;
    }

    void Update()
    {
        if (!isSetup) return;

        if (health != null && healthSlider != null)
        {
            healthSlider.maxValue = health.MaxHealth;
            healthSlider.value = health.currentHealth;
            healthText.text = $"HP: {Mathf.CeilToInt(health.currentHealth)} / {health.MaxHealth}";
        }

        if (resources != null)
        {
            if (manaSlider != null) {
                manaSlider.maxValue = resources.maxMana;
                manaSlider.value = resources.currentMana;
                manaText.text = $"MP: {Mathf.CeilToInt(resources.currentMana)} / {Mathf.CeilToInt(resources.maxMana)}";
            }

            if (staminaSlider != null) {
                staminaSlider.maxValue = resources.maxStamina;
                staminaSlider.value = resources.currentStamina;
                staminaText.text = $"SP: {Mathf.CeilToInt(resources.currentStamina)} / {Mathf.CeilToInt(resources.maxStamina)}";
            }

            if (ultimateSlider != null) {
                ultimateSlider.maxValue = resources.maxUltimate;
                ultimateSlider.value = resources.currentUltimate;
                if (resources.currentUltimate >= resources.maxUltimate) {
                    ultimateText.text = "ULTIMATE READY!";
                    ultimateFill.color = Color.white;
                } else {
                    ultimateText.text = $"ULT: {Mathf.FloorToInt(resources.currentUltimate)}%";
                    ultimateFill.color = new Color(1f, 0.5f, 0f);
                }
            }
        }

        if (inventory != null && inventory.slots != null)
        {
            float cdPercent = 0f;

            for (int i = 0; i < 3; i++)
            {
                if (i < inventory.slots.Length && inventory.slots[i] != null)
                {
                    slotIcons[i].sprite = inventory.slots[i].inventoryIcon;
                    slotIcons[i].color = Color.white;
                    slotCooldowns[i].fillAmount = cdPercent;
                }
                else
                {
                    slotIcons[i].color = new Color(1, 1, 1, 0);
                    slotCooldowns[i].fillAmount = 0;
                }
            }

            // Update Tooltip
            if (inventory.targetedItem != null && inventory.targetedItem.itemData != null)
            {
                tooltipPanel.SetActive(true);
                string keyHint = player.GetComponent<PlayerInput>().currentControlScheme == "Gamepad" ? "Button West" : "Pick Up Key";
                tooltipText.text = $"{inventory.targetedItem.itemData.itemName}\n<size=12>Press [{keyHint}]</size>";
            }
            else
            {
                tooltipPanel.SetActive(false);
            }
        }

        // Update Stats Panel (Top Left)
        if (statsText != null && player != null && player.currentClass != null)
        {
            string mRegen = player.currentClass.manaRegenRate.ToString("F1");
            string sRegen = player.currentClass.staminaRegenRate.ToString("F1");
            string speed = player.MaxSpeed.ToString("F1");

            statsText.text = $"Mana Regen: {mRegen}/s\n" +
                             $"Stamina Regen: {sRegen}/s\n" +
                             $"Max Speed: {speed} m/s";
        }

        // Update Buff Bars (Top Center)
        if (buffManager != null)
        {
            UpdateBuffBar(speedBuffBar, buffManager.speedBuffTimeLeft, buffManager.maxSpeedBuffDuration);
            UpdateBuffBar(staminaBuffBar, buffManager.staminaBuffTimeLeft, buffManager.maxStaminaBuffDuration);
            UpdateBuffBar(infiniteManaBuffBar, buffManager.manaBuffTimeLeft, buffManager.maxManaBuffDuration);
            UpdateBuffBar(healthRegenBuffBar, buffManager.healthRegenBuffTimeLeft, buffManager.maxHealthRegenBuffDuration);
            UpdateBuffBar(manaRegenBuffBar, buffManager.manaRegenBuffTimeLeft, buffManager.maxManaRegenBuffDuration);
            UpdateBuffBar(staminaRegenBuffBar, buffManager.staminaRegenBuffTimeLeft, buffManager.maxStaminaRegenBuffDuration);
        }

        // Update Threat Display (Top Center)
        if (threatText != null)
        {
            float time = LevelManager.runTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            string threatName = LevelManager.GetThreatName();
            Color threatCol = LevelManager.GetThreatColor();
            string colorHex = ColorUtility.ToHtmlStringRGB(threatCol);
            
            threatText.text = $"TIME: {timeString} | THREAT: <color=#{colorHex}><b>{threatName}</b></color>";
        }

        // Update Artifacts Display
        var am = player.GetComponent<ArtifactManager>();
        if (am != null && am.collectedArtifacts.Count != lastArtifactsCount)
        {
            lastArtifactsCount = am.collectedArtifacts.Count;
            RefreshArtifactDisplay();
        }
    }

    private void UpdateBuffBar(Slider s, float timeLeft, float maxDuration)
    {
        if (timeLeft > 0)
        {
            s.gameObject.SetActive(true);
            s.maxValue = maxDuration;
            s.value = timeLeft;
        }
        else
        {
            if (s != null && s.gameObject.activeSelf) s.gameObject.SetActive(false);
        }
    }

    public void RefreshArtifactDisplay()
    {
        foreach (var go in artifactIconGOs)
        {
            if (go != null) Destroy(go);
        }
        artifactIconGOs.Clear();

        var artifactManager = player.GetComponent<ArtifactManager>();
        if (artifactManager == null) return;

        var groups = new System.Collections.Generic.Dictionary<string, (ArtifactSO artifact, int count)>();
        foreach (var a in artifactManager.collectedArtifacts)
        {
            if (groups.ContainsKey(a.itemName))
            {
                groups[a.itemName] = (groups[a.itemName].artifact, groups[a.itemName].count + 1);
            }
            else
            {
                groups[a.itemName] = (a, 1);
            }
        }

        foreach (var pair in groups.Values)
        {
            GameObject slotGO = new GameObject($"ArtifactSlot_{pair.artifact.itemName}", typeof(RectTransform));
            slotGO.transform.SetParent(artifactPanel.transform, false);
            artifactIconGOs.Add(slotGO);

            RectTransform slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(40, 40);

            var bgImg = slotGO.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);

            GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = pair.artifact.inventoryIcon;
            iconImg.color = Color.white;

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            if (pair.count > 1)
            {
                GameObject countGO = new GameObject("CountText", typeof(RectTransform));
                countGO.transform.SetParent(slotGO.transform, false);
                
                var countText = countGO.AddComponent<TextMeshProUGUI>();
                countText.text = $"x{pair.count}";
                countText.fontSize = 12;
                countText.alignment = TextAlignmentOptions.BottomRight;
                countText.color = Color.yellow;
                countText.fontStyle = FontStyles.Bold;

                RectTransform countRect = countGO.GetComponent<RectTransform>();
                countRect.anchorMin = Vector2.zero;
                countRect.anchorMax = Vector2.one;
                countRect.sizeDelta = Vector2.zero;
            }
        }
    }
}
