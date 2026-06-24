using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages AR texture visualization for flooring and wall paneling.
/// Downloads a texture and applies it to detected AR planes with realistic tiling.
/// Performance-optimized: shared material instance, lazy updates, cached plane references.
/// </summary>
public class TexturePlacementManager : MonoBehaviour
{
    public enum TextureTarget { Floor, Wall, Both }

    [Header("AR References")]
    private ARPlaneManager planeManager;

    [Header("Tutorial Text")]
    public TextMeshProUGUI tutorialText;

    [Header("Texture Settings")]
    [Tooltip("Real-world size of one texture tile in meters (e.g., 0.6 = 60cm tile)")]
    public float realWorldTileSize = 0.6f;

    [Tooltip("Texture filtering mode for quality")]
    public FilterMode textureFilterMode = FilterMode.Trilinear;

    [Tooltip("Anisotropic filtering level (1-16). Higher = sharper at oblique angles)")]
    [Range(1, 16)]
    public int anisoLevel = 8;

    [Header("Placement Options")]
    [Tooltip("If true, textures automatically apply to scanned planes. If false, textures must be applied manually via screen tap or Place button.")]
    public bool autoApplyToAllPlanes = false;

    // ── State ──
    private bool isActive = false;
    private TextureTarget currentTarget = TextureTarget.Floor;
    private Texture2D activeTexture;
    private Material textureMaterial; // Single shared material — applied to all planes
    private ProductData activeProductData;

    // ── Plane tracking (avoid per-frame alloc) ──
    private readonly Dictionary<ARPlane, Material[]> originalMaterials = new Dictionary<ARPlane, Material[]>();
    private readonly HashSet<ARPlane> texturedPlanes = new HashSet<ARPlane>();

    // ── Singleton (optional, for easy access from AppUIManager) ──
    public static TexturePlacementManager Instance { get; private set; }

    void Awake()
    {
        float awakeStart = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {awakeStart:F4}s) --- TexturePlacementManager Awake started ---");
        Instance = this;
        planeManager = GetComponent<ARPlaneManager>();
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) TexturePlacementManager Awake completed in {Time.realtimeSinceStartup - awakeStart:F4}s");
    }

    void Start()
    {
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {start:F4}s) --- TexturePlacementManager Start started ---");
        if (!isActive && tutorialText != null)
        {
            tutorialText.text = "";
        }
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) TexturePlacementManager Start completed in {Time.realtimeSinceStartup - start:F4}s");
    }

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnDestroy()
    {
        CleanupMaterial();
    }

    // ═══════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Activate texture mode. Downloads texture from thumbnailUrl, creates a shared material,
    /// and starts applying it to matching AR planes.
    /// </summary>
    public void Activate(Texture2D texture, ProductData productData)
    {
        if (texture == null)
        {
            Debug.LogError("TexturePlacementManager: Cannot activate with null texture!");
            return;
        }

        activeTexture = texture;
        activeProductData = productData;
        isActive = true;

        // Auto-detect target based on category
        if (productData != null)
        {
            if (productData.IsWallTexture && !productData.IsFloorTexture)
                currentTarget = TextureTarget.Wall;
            else if (productData.IsFloorTexture && !productData.IsWallTexture)
                currentTarget = TextureTarget.Floor;
            else
                currentTarget = TextureTarget.Floor; // Default to floor
        }

        // Optimize texture for AR rendering
        ConfigureTexture(activeTexture);

        // Create the shared tiling material
        CreateSharedMaterial();

        // Apply to all existing planes that match (if auto-apply is enabled)
        if (autoApplyToAllPlanes)
        {
            ApplyToExistingPlanes();
        }

        UpdateTutorialText();

        Debug.Log($"TexturePlacementManager: Activated for '{productData?.name}'. Target: {currentTarget}");
    }

    /// <summary>
    /// Pause texture mode (hide UI and stop tracking), but LEAVE the applied textures in the AR world!
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        if (tutorialText != null)
            tutorialText.text = "";

        Debug.Log("TexturePlacementManager: Paused tracking. Textures are persisting in the background.");
    }

    /// <summary>
    /// Completely strip all textures from planes and destroy the material.
    /// </summary>
    public void ClearTextures()
    {
        isActive = false;
        RestoreAllPlanes();
        CleanupMaterial();
        activeTexture = null;
        activeProductData = null;

        if (tutorialText != null)
            tutorialText.text = "";

        Debug.Log("TexturePlacementManager: Completely cleared all textures from planes.");
    }

    /// <summary>
    /// Toggle between Floor and Wall target.
    /// </summary>
    public void ToggleTarget()
    {
        if (!isActive) return;

        if (currentTarget == TextureTarget.Floor)
            currentTarget = TextureTarget.Wall;
        else
            currentTarget = TextureTarget.Floor;

        // Restore all, then re-apply to matching planes (if auto-apply is enabled)
        RestoreAllPlanes();
        if (autoApplyToAllPlanes)
        {
            ApplyToExistingPlanes();
        }
        UpdateTutorialText();

        Debug.Log($"TexturePlacementManager: Switched target to {currentTarget}");
    }

    /// <summary>
    /// Set target mode explicitly.
    /// </summary>
    public void SetTarget(TextureTarget target)
    {
        if (!isActive) return;
        if (currentTarget == target) return;

        currentTarget = target;
        RestoreAllPlanes();
        if (autoApplyToAllPlanes)
        {
            ApplyToExistingPlanes();
        }
        UpdateTutorialText();
    }

    /// <summary>
    /// Returns true if texture mode is currently active.
    /// </summary>
    public bool IsActive => isActive;

    /// <summary>
    /// Returns the current target type.
    /// </summary>
    public TextureTarget CurrentTarget => currentTarget;

    // ═══════════════════════════════════════════
    // TEXTURE & MATERIAL SETUP
    // ═══════════════════════════════════════════

    /// <summary>
    /// Configure texture import settings for optimal AR rendering.
    /// </summary>
    private void ConfigureTexture(Texture2D tex)
    {
        if (tex == null) return;

        tex.filterMode = textureFilterMode;
        tex.anisoLevel = anisoLevel;
        tex.wrapMode = TextureWrapMode.Repeat;
    }

    /// <summary>
    /// Create a single shared material with tiling. All planes reference this one material
    /// to minimize GPU state changes and draw calls.
    /// </summary>
    private void CreateSharedMaterial()
    {
        CleanupMaterial();

        // Search for a suitable shader depending on project configuration (prefer Custom/WorldSpaceTexture for seamless tiles)
        Shader shader = Shader.Find("Custom/WorldSpaceTexture");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            Debug.LogError("TexturePlacementManager: Could not find any valid shader (Custom/WorldSpaceTexture, Universal Render Pipeline/Lit, Standard, or Unlit/Texture)!");
            return;
        }

        textureMaterial = new Material(shader);
        textureMaterial.name = "AR_TexturePreview";
        textureMaterial.mainTexture = activeTexture;
        
        // Configure properties depending on the loaded shader
        if (shader.name.Contains("WorldSpaceTexture"))
        {
            textureMaterial.SetFloat("_TileScale", 1.0f); // Default 1 repeat per meter
            textureMaterial.SetFloat("_Glossiness", 0.3f);
            textureMaterial.SetFloat("_Metallic", 0.0f);
        }
        else if (shader.name.Contains("Lit") || shader.name.Contains("Standard"))
        {
            // Use properties that exist in Standard/URP Lit shaders
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                textureMaterial.SetFloat("_Smoothness", 0.3f);
                textureMaterial.SetFloat("_Metallic", 0.0f);
            }
            else
            {
                // Built-in Standard shader properties
                textureMaterial.SetFloat("_Glossiness", 0.3f);
                textureMaterial.SetFloat("_Metallic", 0.0f);
            }
        }

        // Initial tiling — will be recalculated per-plane in ApplyToPlane
        textureMaterial.mainTextureScale = Vector2.one;
    }

    /// <summary>
    /// Destroy the shared material to free GPU memory.
    /// </summary>
    private void CleanupMaterial()
    {
        if (textureMaterial != null)
        {
            Destroy(textureMaterial);
            textureMaterial = null;
        }
    }

    // ═══════════════════════════════════════════
    // PLANE MANAGEMENT
    // ═══════════════════════════════════════════

    /// <summary>
    /// Called by ARPlaneManager when planes are added, updated, or removed.
    /// </summary>
    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (!isActive) return;

        // Apply texture to newly detected planes (if auto-apply is enabled)
        foreach (var plane in args.added)
        {
            if (autoApplyToAllPlanes && ShouldTexturePlane(plane))
            {
                ApplyToPlane(plane);
            }
        }

        // Update tiling on planes that changed size
        foreach (var plane in args.updated)
        {
            // If this textured plane was subsumed by a larger plane, automatically texture the new plane!
            if (plane.subsumedBy != null && texturedPlanes.Contains(plane))
            {
                ARPlane subsumingPlane = plane.subsumedBy;
                if (!texturedPlanes.Contains(subsumingPlane))
                {
                    Debug.Log($"TexturePlacementManager: Plane {plane.trackableId} subsumed by {subsumingPlane.trackableId}. Automatically applying texture to the new plane.");
                    ApplyToPlane(subsumingPlane);
                }
            }

            if (texturedPlanes.Contains(plane))
            {
                UpdatePlaneTiling(plane);
            }
            else if (autoApplyToAllPlanes && ShouldTexturePlane(plane))
            {
                // Plane classification might have changed (if auto-apply is enabled)
                ApplyToPlane(plane);
            }
        }

        // Clean up removed planes
        foreach (var plane in args.removed)
        {
            if (plane != null)
            {
                originalMaterials.Remove(plane);
                texturedPlanes.Remove(plane);
            }
        }
    }

    /// <summary>
    /// Check if a plane should receive the texture based on current target setting.
    /// </summary>
    private bool ShouldTexturePlane(ARPlane plane)
    {
        if (plane == null) return false;

        PlaneAlignment alignment = plane.alignment;

        switch (currentTarget)
        {
            case TextureTarget.Floor:
                return alignment == PlaneAlignment.HorizontalUp;
            case TextureTarget.Wall:
                return alignment == PlaneAlignment.Vertical;
            case TextureTarget.Both:
                return alignment == PlaneAlignment.HorizontalUp || alignment == PlaneAlignment.Vertical;
            default:
                return false;
        }
    }

    public void ApplyToPlane(ARPlane plane)
    {
        if (plane == null || textureMaterial == null) return;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        // Ensure AppUIManager has cached the original plane materials (including the border)
        // before we modify the renderer's materials array
        if (AppUIManager.Instance != null && !AppUIManager.Instance.originalPlaneMaterials.ContainsKey(plane))
        {
            if (renderer.materials.Length > 1)
            {
                AppUIManager.Instance.originalPlaneMaterials[plane] = renderer.materials;
            }
        }

        // Save original materials (only if not already saved)
        if (!originalMaterials.ContainsKey(plane))
        {
            originalMaterials[plane] = renderer.materials; // Clone the array
        }

        // Determine the textured material to apply
        Material appliedMat;
        if (textureMaterial.shader.name.Contains("WorldSpaceTexture"))
        {
            appliedMat = textureMaterial;
        }
        else
        {
            // Fallback for standard shaders (need per-plane instance for tiling)
            appliedMat = new Material(textureMaterial);
            appliedMat.mainTextureScale = Vector2.one;
        }

        renderer.material = appliedMat;

        // Sync visibility and border outline states with AppUIManager
        if (AppUIManager.Instance != null)
        {
            AppUIManager.Instance.ApplyPlaneVisibility(plane, AppUIManager.Instance.ARPlanesVisible);
        }

        texturedPlanes.Add(plane);
    }

    /// <summary>
    /// Update the tiling of an already-textured plane when its size changes.
    /// Re-applies the texture if AR Foundation resets the material.
    /// </summary>
    private void UpdatePlaneTiling(ARPlane plane)
    {
        if (plane == null) return;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        // If AR Foundation reset the material (e.g. during mesh visualizer update), re-apply it!
        if (renderer.sharedMaterial == null || !renderer.sharedMaterial.name.Contains("AR_TexturePreview"))
        {
            ApplyToPlane(plane);
            return;
        }

        if (textureMaterial != null && textureMaterial.shader.name.Contains("WorldSpaceTexture"))
        {
            return; // World-space shader handles scaling dynamically in world space
        }

        renderer.material.mainTextureScale = Vector2.one;
    }

    /// <summary>
    /// Apply texture to all existing planes that match the current target.
    /// </summary>
    private void ApplyToExistingPlanes()
    {
        if (planeManager == null) return;

        foreach (var plane in planeManager.trackables)
        {
            if (ShouldTexturePlane(plane) && !texturedPlanes.Contains(plane))
            {
                ApplyToPlane(plane);
            }
        }
    }

    /// <summary>
    /// Restore all planes to their original materials.
    /// </summary>
    private void RestoreAllPlanes()
    {
        foreach (var kvp in originalMaterials)
        {
            ARPlane plane = kvp.Key;
            Material[] origMats = kvp.Value;

            if (plane == null) continue;

            MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            // Destroy the per-plane material instance to free memory
            if (renderer.material != null && renderer.material.name.Contains("AR_TexturePreview"))
            {
                Destroy(renderer.material);
            }

            renderer.materials = origMats;
        }

        originalMaterials.Clear();
        texturedPlanes.Clear();
    }

    // ═══════════════════════════════════════════
    // UI
    // ═══════════════════════════════════════════

    private void UpdateTutorialText()
    {
        if (tutorialText == null) return;

        string productName = activeProductData?.name ?? "Texture";

        switch (currentTarget)
        {
            case TextureTarget.Floor:
                tutorialText.text = $"Scan the floor to preview\n<b>{productName}</b>";
                break;
            case TextureTarget.Wall:
                tutorialText.text = $"Scan walls to preview\n<b>{productName}</b>";
                break;
            case TextureTarget.Both:
                tutorialText.text = $"Scan surfaces to preview\n<b>{productName}</b>";
                break;
        }
    }
}
