using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(InventoryManager))]
[RequireComponent(typeof(ResourceSystem))]
[RequireComponent(typeof(BuffManager))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public CharacterClassSO currentClass;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float cameraHeight = 1.8f; // Height of the pivot
    [SerializeField] private float cameraSideOffset = 0.6f; // Left/Right
    [SerializeField] private float cameraVerticalOffset = 0.3f; // Up/Down relative to pivot
    [SerializeField] private float cameraDistance = 3.5f; // Distance behind
    [SerializeField] private float lookSpeed = 4f;
    [SerializeField] private float minViewAngle = -40f;
    [SerializeField] private float maxViewAngle = 60f;

    [Header("Global Physics")]
    [SerializeField] private float gravityValue = -15.0f;
    [SerializeField] private float acceleration = 12.0f;
    [SerializeField] private float friction = 10.0f;

    private CharacterController charController;
    public Animator animator { get; private set; }
    private ResourceSystem resourceSystem;
    private BuffManager buffManager;
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 playerVelocity;
    public Vector3 currentVelocity { get; private set; }
    public float CurrentSpeed => currentVelocity.magnitude;
    public float MaxSpeed => currentClass != null ? currentClass.baseMovementSpeed * currentClass.sprintMultiplier : 0f;
    public float damageMultiplier { get; set; } = 1.0f;
    public float meleeRangeBonus { get; set; } = 0f;
    private bool isGrounded;
    public bool isSprinting { get; private set; }
    public bool isActuallySprinting { get; private set; }
    private bool isLookingBehind = false;
    private float rotationY;
    private float rotationX;
    
    public float externalModelRotationY { get; set; }
    private Quaternion currentLean = Quaternion.identity;
    
    public Camera playerCamera { get; private set; }

    private int playerIndex = -1;
    private int myUIReportLayer;

    private PlayerInput playerInput;
    private GameObject characterModel;
    public GameObject activeWeapon { get; private set; }

    // Stability: Use a follower strategy instead of parenting to avoid Prefab Variant crashes
    private List<GameObject> activeModels = new List<GameObject>();

    void Awake()
    {
        charController = GetComponent<CharacterController>();
        resourceSystem = GetComponent<ResourceSystem>();
        buffManager = GetComponent<BuffManager>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            playerIndex = playerInput.playerIndex;
            myUIReportLayer = 20 + playerIndex;
        }

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            if (cameraPivot == null)
            {
                GameObject pivotGO = new GameObject("CameraPivot");
                pivotGO.transform.SetParent(this.transform);
                cameraPivot = pivotGO.transform;
            }
            
            // Force the pivot to the correct eye-level height
            cameraPivot.localPosition = new Vector3(0, cameraHeight, 0);
            
            playerCamera.transform.SetParent(cameraPivot);

            for (int i = 0; i < 4; i++)
            {
                int layer = 20 + i;
                if (layer == myUIReportLayer)
                    playerCamera.cullingMask |= (1 << layer); 
                else
                    playerCamera.cullingMask &= ~(1 << layer); 
            }
            
            if (playerCamera.gameObject.GetComponent<CrosshairUI>() == null)
                playerCamera.gameObject.AddComponent<CrosshairUI>();
                
            if (playerCamera.gameObject.GetComponent<SplitScreenCamera>() == null)
                playerCamera.gameObject.AddComponent<SplitScreenCamera>();

            // Deduplicate HUD component
            var hud = GetComponent<PlayerHUD>();
            if (hud == null) hud = gameObject.AddComponent<PlayerHUD>();
            hud.Setup(this, GetComponent<Health>(), resourceSystem, playerCamera);
        }
    }

    public void InitializeClass(CharacterClassSO selectedClass)
    {
        if (selectedClass == null) return;

#if UNITY_EDITOR
        // HACK: Deselect the player to prevent the Unity Inspector from panicking 
        // while we swap models and update the hierarchy.
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Selection.activeGameObject = null;
        }
#endif

        StartCoroutine(InitializeClassRoutine(selectedClass));
    }

    private System.Collections.IEnumerator InitializeClassRoutine(CharacterClassSO selectedClass)
    {
        // One frame delay to allow Unity to finish its current refresh cycle
        yield return null;

        // Cleanup the previous runtime class instance
        currentClass = null;

        currentClass = Instantiate(selectedClass);
        currentClass.name = selectedClass.name; 
        
        var health = GetComponent<Health>();
        if (health != null)
        {
            health.isPlayer = true;
            health.SetMaxHealth(currentClass.maxHealth);
        }

        if (resourceSystem != null) resourceSystem.InitializeResources(selectedClass);
        
        var rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;
        
        // Surgical cleanup of previous model
        if (activeWeapon != null)
        {
            Destroy(activeWeapon);
            activeWeapon = null;
        }

        if (characterModel != null)
        {
            characterModel.name = "DELETED_MODEL_CLEANUP";
            characterModel.SetActive(false);
            Destroy(characterModel);
            characterModel = null;
        }

        if (currentClass.characterModelPrefab != null)
        {
            characterModel = Instantiate(currentClass.characterModelPrefab, transform);
            characterModel.transform.localPosition = currentClass.modelOffset;
            characterModel.transform.localRotation = Quaternion.identity;
            characterModel.transform.localScale = Vector3.one * currentClass.modelScale;
            
            animator = characterModel.GetComponent<Animator>();
            if (animator == null) animator = characterModel.GetComponentInChildren<Animator>();

            // Instantiate weapon and attach to hand bone
            if (currentClass.weaponPrefab != null && animator != null && animator.isHuman)
            {
                Transform hand = animator.GetBoneTransform(currentClass.handBone);
                if (hand != null)
                {
                    GameObject weapon = Instantiate(currentClass.weaponPrefab, hand);
                    weapon.transform.localPosition = currentClass.weaponPositionOffset;
                    weapon.transform.localRotation = Quaternion.Euler(currentClass.weaponRotationOffset);
                }
            }
        }

        UpdateCameraPerspective();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && currentClass != null)
        {
            playerVelocity.y = Mathf.Sqrt(currentClass.jumpHeight * -3.0f * gravityValue);
            SetAnimatorTrigger("Jump");
        }
    }

    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    public void OnToggleCamera(InputValue value)
    {
        isLookingBehind = value.isPressed;
        UpdateCameraPerspective();
    }

    private void UpdateCameraPerspective()
    {
        if (playerCamera == null) return;

        if (isLookingBehind)
        {
            // Look Behind: Camera in front, looking backwards
            playerCamera.transform.localPosition = new Vector3(0, 0, 4f);
            playerCamera.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            SetModelVisible(true);
        }
        else
        {
            // Third Person: Over-the-shoulder (Customizable offsets)
            playerCamera.transform.localPosition = new Vector3(cameraSideOffset, cameraVerticalOffset, -cameraDistance);
            playerCamera.transform.localRotation = Quaternion.identity;
            SetModelVisible(true);
        }
    }

    private void SetModelVisible(bool visible)
    {
        if (characterModel == null) return;
        foreach (var r in characterModel.GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
    }

    public void PerformDash(float distance, float duration)
    {
        StartCoroutine(DashCoroutine(distance, duration));
    }

    private System.Collections.IEnumerator DashCoroutine(float distance, float duration)
    {
        Vector3 dashDir = GetMovementDirection();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float step = (distance / duration) * Time.deltaTime;
            charController.Move(dashDir * step);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public Vector3 GetMovementDirection()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude < 0.01f) return transform.forward;
        return move.normalized;
    }

    public Vector3 GetAimPoint(float maxDistance = 50f)
    {
        if (playerCamera == null) return transform.position + transform.forward * maxDistance;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            return hit.point;
        }
        return ray.origin + ray.direction * maxDistance;
    }

    void Update()
    {
        if (currentClass == null) return;

        UpdateHealthBarRaycast();

        isGrounded = charController.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // --- Rotation ---
        if (lookInput.sqrMagnitude > 0.01f)
        {
            rotationY += lookInput.x * lookSpeed * Time.deltaTime * 100f;
            transform.localEulerAngles = new Vector3(0, rotationY, 0);

            // Invert Y-axis if looking behind
            float yInput = isLookingBehind ? -lookInput.y : lookInput.y;
            rotationX -= yInput * lookSpeed * Time.deltaTime * 100f;
            
            rotationX = Mathf.Clamp(rotationX, minViewAngle, maxViewAngle);
            if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(rotationX, 0, 0);
        }

        // --- Movement & Momentum ---
        float targetSpeed = currentClass.baseMovementSpeed;
        
        bool sprintHeld = isSprinting;
        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Sprint") != null)
        {
            sprintHeld = playerInput.currentActionMap.FindAction("Sprint").IsPressed();
        }

        isActuallySprinting = sprintHeld && resourceSystem.currentStamina > 0.5f && moveInput.sqrMagnitude > 0.01f;
        
        if (isActuallySprinting)
        {
            targetSpeed *= currentClass.sprintMultiplier;
            
            // Only drain stamina if infinite buff is NOT active
            if (buffManager == null || !buffManager.isInfiniteStaminaActive)
            {
                resourceSystem.UseStamina(currentClass.staminaSprintCost * Time.deltaTime);
            }
        }

        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDir * targetSpeed;

        float lerpFactor = moveInput.sqrMagnitude > 0.01f ? acceleration : friction;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpFactor * Time.deltaTime);

        charController.Move(currentVelocity * Time.deltaTime);

        // --- Gravity ---
        playerVelocity.y += gravityValue * Time.deltaTime;
        charController.Move(playerVelocity * Time.deltaTime);
        
        // --- Procedural Juicing ---
        if (characterModel != null)
        {
            float forwardLean = (currentVelocity.magnitude / (currentClass.baseMovementSpeed * 2)) * 15f;
            float sideTilt = -moveInput.x * 10f;
            
            // Smoothly calculate lean
            Quaternion targetLean = Quaternion.Euler(forwardLean, 0, sideTilt);
            currentLean = Quaternion.Slerp(currentLean, targetLean, 5f * Time.deltaTime);
            
            // Smoothly dampen the external rotation back to 0 if the ability is not actively pushing it
            // (e.g. slowing down after the Beyblade spin)
            var combat = GetComponent<CombatSystem>();
            if (combat == null || !combat.IsHoldingAbility())
            {
                externalModelRotationY = Mathf.Lerp(externalModelRotationY, 0f, 10f * Time.deltaTime);
                
                // Snap to exactly 0 to prevent micro-rotations forever
                if (Mathf.Abs(externalModelRotationY) < 0.1f) externalModelRotationY = 0f;
            }
            
            // Apply lean and external ability spins (like Whirlwind)
            characterModel.transform.localRotation = currentLean * Quaternion.Euler(0, externalModelRotationY, 0);
        }

        // --- Animation ---
        if (animator != null)
        {
            float speedPercent = 0f;
            float animSpeedMultiplier = 1f;

            if (isGrounded && moveInput.sqrMagnitude > 0.01f)
            {
                float actualSpeedMag = currentVelocity.magnitude;
                float baseSpeed = currentClass.baseMovementSpeed;
                float maxSpeed = baseSpeed * currentClass.sprintMultiplier;
                
                speedPercent = Mathf.Clamp01(actualSpeedMag / maxSpeed);
                if (speedPercent < 0.2f) speedPercent = 0.4f; 

                // Use the reference speed to calculate how fast the legs should move
                animSpeedMultiplier = actualSpeedMag / currentClass.referenceAnimSpeed;

                // SAFETY CAP: Prevent animations from looking like a blur if speed is too high
                animSpeedMultiplier = Mathf.Min(animSpeedMultiplier, 2.0f);
            }
            
            if (HasParameter(animator, "MovementSpeed"))
                animator.SetFloat("MovementSpeed", speedPercent, 0.05f, Time.deltaTime);
            
            if (HasParameter(animator, "AnimSpeedMultiplier"))
                animator.SetFloat("AnimSpeedMultiplier", animSpeedMultiplier);

            if (HasParameter(animator, "IsGrounded"))
                animator.SetBool("IsGrounded", isGrounded);

            if (HasParameter(animator, "VerticalVelocity"))
                animator.SetFloat("VerticalVelocity", playerVelocity.y);
        }
    }

    private void UpdateHealthBarRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (resourceSystem.GetComponent<InventoryManager>() != null)
            resourceSystem.GetComponent<InventoryManager>().SetTargetedItem(null);

        // Force raycast to hit triggers so it can see the item colliders
        if (Physics.Raycast(ray, out RaycastHit hit, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            EnemyHealthBar hp = hit.collider.GetComponent<EnemyHealthBar>();
            if (hp == null) hp = hit.collider.GetComponentInParent<EnemyHealthBar>();
            if (hp != null) hp.ShowForPlayer(playerIndex, myUIReportLayer);

            PickupItem item = hit.collider.GetComponent<PickupItem>();
            if (item == null) item = hit.collider.GetComponentInParent<PickupItem>();
            if (item != null)
            {
                var inv = GetComponent<InventoryManager>();
                if (inv != null) inv.SetTargetedItem(item);
            }
        }
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator != null && HasParameter(animator, triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
}
