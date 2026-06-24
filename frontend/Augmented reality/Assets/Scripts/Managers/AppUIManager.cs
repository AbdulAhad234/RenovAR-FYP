using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;
using System.Threading.Tasks;

public class AppUIManager : MonoBehaviour
{
    public static AppUIManager Instance;

    [Header("UI Panels")]
    public GameObject cataloguePanel;
    public GameObject detailsPanel;
    public GameObject arHUDPanel;
    
    [Header("Prefabs")]
    public GameObject loadingPanelPrefab; 
    [HideInInspector] public LoadingPanelUI activeLoadingPanel;

    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public Material occlusionMaskMaterial; // Assign your Custom_OcclusionMask material here

    private bool arPlanesVisible = true;
    public bool ARPlanesVisible => arPlanesVisible;
    [HideInInspector] public System.Collections.Generic.Dictionary<ARPlane, Material[]> originalPlaneMaterials = new System.Collections.Generic.Dictionary<ARPlane, Material[]>();
    private ProductDetailsUI detailsUI;

    void Awake()
    {
        float awakeTime = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {awakeTime:F4}s) --- AppUIManager Awake started ---");
        Instance = this;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) AppUIManager Awake completed");
    }

    void Start()
    {
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {start:F4}s) --- AppUIManager Start started ---");
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();

        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }

        // Check if we have a pre-selected product (e.g., loaded from Homepage scene)
        ProductData selected = SelectedProductHolder.Instance != null ? SelectedProductHolder.Instance.GetSelectedProductData() : null;
        if (selected != null)
        {
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Pre-selected product '{selected.name}' found in Start. Triggering ShowDetails...");
            ShowDetails(selected);
        }
        else
        {
            // Force panels to correct state at start
            if (cataloguePanel != null) cataloguePanel.SetActive(true);
            if (detailsPanel != null) detailsPanel.SetActive(false);
            if (arHUDPanel != null) arHUDPanel.SetActive(false);
            ToggleAR(false);
        }
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) --- AppUIManager Start completed in {Time.realtimeSinceStartup - start:F4}s ---");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // When grid is hidden, apply the correct visibility to any newly detected planes
        if (!arPlanesVisible)
        {
            foreach (var plane in args.added)
            {
                ApplyPlaneVisibility(plane, false);
            }
        }
    }

    /// <summary>
    /// Call this from the Show Grid toggle's OnValueChanged event.
    /// </summary>
    public void TogglePlaneVisibility(bool visible)
    {
        arPlanesVisible = visible;

        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                ApplyPlaneVisibility(plane, visible);
            }
        }
    }

    /// <summary>
    /// Applies visibility rules per plane type:
    /// - Horizontal planes (floor/ceiling): fully disabled when hidden (except in ProductTextureScene)
    /// - Vertical planes (walls) and horizontal planes in ProductTextureScene: switch to occlusion material so they mask objects behind them
    /// </summary>
    public void ApplyPlaneVisibility(ARPlane plane, bool visible)
    {
        bool isTextureScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ProductTextureScene";
        bool isVertical = (plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.Vertical);

        MeshRenderer mr = plane.GetComponent<MeshRenderer>();
        if (mr == null) return;

        bool hasTexture = false;
        Material textureMat = null;
        foreach (var mat in mr.sharedMaterials)
        {
            if (mat != null && mat.name.Contains("AR_TexturePreview"))
            {
                hasTexture = true;
                textureMat = mat;
                break;
            }
        }

        if (visible)
        {
            // Show everything normally
            plane.gameObject.SetActive(true);

            if (hasTexture)
            {
                // Find original border material if we have it cached or currently present
                Material borderMat = null;
                if (originalPlaneMaterials.ContainsKey(plane) && originalPlaneMaterials[plane].Length > 1)
                {
                    borderMat = originalPlaneMaterials[plane][1];
                }
                else if (mr.sharedMaterials.Length > 1)
                {
                    borderMat = mr.sharedMaterials[1];
                }

                if (borderMat != null && textureMat != null)
                {
                    mr.materials = new Material[] { textureMat, borderMat };
                }
            }
            else
            {
                // Restore the full materials array if no texture is applied
                if (originalPlaneMaterials.ContainsKey(plane))
                {
                    mr.materials = originalPlaneMaterials[plane];
                }
            }
        }
        else
        {
            if (isVertical || isTextureScene)
            {
                // Keep the plane ACTIVE but remove the visible border material (index 1)
                // Only the occlusion mask (index 0) remains, so planes are invisible but still mask objects or show texture
                plane.gameObject.SetActive(true);

                if (hasTexture)
                {
                    // If a texture is applied, keep only the texture material (index 0) and remove the border
                    if (textureMat != null)
                    {
                        mr.materials = new Material[] { textureMat };
                    }
                }
                else
                {
                    // Cache the full original materials array so we can restore it later
                    if (!originalPlaneMaterials.ContainsKey(plane))
                    {
                        originalPlaneMaterials[plane] = mr.materials;
                    }

                    // Keep only the first material (occlusion mask), drop the border material
                    if (mr.materials.Length > 1)
                    {
                        mr.materials = new Material[] { mr.materials[0] };
                    }
                }
            }
            else
            {
                // Horizontal planes in non-texture scenes (e.g. ProductModelScene): fully hide
                plane.gameObject.SetActive(false);
            }
        }
    }

    public void ShowCatalogue()
    {
        if (cataloguePanel != null) cataloguePanel.SetActive(true);
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (arHUDPanel != null) arHUDPanel.SetActive(false);
        
        ToggleAR(false);

        // Clear pre-selected product so that going back from catalogue goes to home, not details
        if (SelectedProductHolder.Instance != null)
        {
            SelectedProductHolder.Instance.SetSelectedProductData(null);
        }

        // Force clear any stuck local back actions to ensure the back button goes to Homepage
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.ClearLocalActions();
        }
    }

    public void ShowCart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("AddToCartScene");
    }

    public void ShowDetails(ProductData data)
    {
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {start:F4}s) --- ShowDetails started for '{data?.name}' ---");
        if (cataloguePanel != null) cataloguePanel.SetActive(false);
        if (detailsPanel != null) detailsPanel.SetActive(true);
        if (arHUDPanel != null) arHUDPanel.SetActive(false);
        
        ToggleAR(false);

        if (detailsUI == null && detailsPanel != null) 
        {
            detailsUI = detailsPanel.GetComponentInChildren<ProductDetailsUI>(true); // true to include inactive
        }
            
        if (detailsUI != null) 
        {
            float setupStart = Time.realtimeSinceStartup;
            detailsUI.Setup(data);
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) ProductDetailsUI Setup completed in {Time.realtimeSinceStartup - setupStart:F4}s");
        }
        else
        {
            Debug.LogError("AppUIManager: Could not find 'ProductDetailsUI' script on your Details Panel! Please make sure you added the script to the panel.");
        }

        // Register local back action to return to catalogue from details
        if (NavigationManager.Instance != null)
        {
            // Pop any existing local back action first to prevent duplicate/stale entries
            NavigationManager.Instance.PopLocalAction();
            NavigationManager.Instance.PushLocalAction(ShowCatalogue);
        }
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) --- ShowDetails completed in {Time.realtimeSinceStartup - start:F4}s ---");
    }

    public void ShowARMode()
    {
        if (cataloguePanel != null) cataloguePanel.SetActive(false);
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (arHUDPanel != null) arHUDPanel.SetActive(true);
        
        ToggleAR(true);

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.PushLocalAction(ExitARMode);
        }
        
        // Check if we're in texture mode
        bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;
        
        if (isTextureMode)
        {
            // Texture mode: activate TexturePlacementManager
            TexturePlacementManager texMgr = FindObjectOfType<TexturePlacementManager>();
            if (texMgr == null)
            {
                // Auto-add to XR Origin if not present
                SurfacePlacementManager spm = FindObjectOfType<SurfacePlacementManager>();
                if (spm != null)
                {
                    texMgr = spm.gameObject.AddComponent<TexturePlacementManager>();
                }
            }

            if (texMgr != null)
            {
                Texture2D tex = SelectedProductHolder.Instance.GetSelectedTexture();
                ProductData pd = SelectedProductHolder.Instance.GetSelectedProductData();
                if (tex != null)
                {
                    texMgr.Activate(tex, pd);
                }
            }

            // Also initialize SurfacePlacementManager so placement preview / inputs run
            SurfacePlacementManager placementManager = FindObjectOfType<SurfacePlacementManager>();
            if (placementManager != null)
            {
                placementManager.OnEnterARMode();
            }
        }
        else
        {
            // Normal 3D model mode: let SurfacePlacementManager handle placement
            // Deactivate TexturePlacementManager to prevent any UI prompt or plane texture overrides
            TexturePlacementManager texMgr = FindObjectOfType<TexturePlacementManager>();
            if (texMgr != null)
            {
                texMgr.Deactivate();
            }

            SurfacePlacementManager placementManager = FindObjectOfType<SurfacePlacementManager>();
            if (placementManager != null)
            {
                placementManager.OnEnterARMode();
            }
        }
    }

    public void ExitARMode()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.RemoveLocalAction(ExitARMode);
        }

        if (arHUDPanel != null) arHUDPanel.SetActive(false);
        ToggleAR(false);

        // Turn off plane visualizer lines/planes
        TogglePlaneVisibility(false);

        // Deactivate texture placement manager
        TexturePlacementManager texMgr = TexturePlacementManager.Instance;
        if (texMgr != null)
        {
            texMgr.Deactivate();
        }

        // Deactivate surface placement manager
        SurfacePlacementManager placementManager = FindObjectOfType<SurfacePlacementManager>();
        if (placementManager != null)
        {
            placementManager.OnExitARMode();
        }

        // Show details panel again (or catalogue if details is empty)
        ProductData selected = SelectedProductHolder.Instance != null ? SelectedProductHolder.Instance.GetSelectedProductData() : null;
        if (selected != null)
        {
            ShowDetails(selected);
        }
        else
        {
            ShowCatalogue();
        }
    }

    public void ShowLoading()
    {
        if (activeLoadingPanel == null && loadingPanelPrefab != null)
        {
            // Instantiate as a sibling to the catalogue panel (under the main Canvas)
            GameObject instance = Instantiate(loadingPanelPrefab, cataloguePanel.transform.parent);
            
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = new Vector3(0.800000012f, 0.734930933f, 0.734930933f);
            }

            activeLoadingPanel = instance.GetComponent<LoadingPanelUI>();
            
            // Put it at the bottom of the hierarchy so it draws on top
            instance.transform.SetAsLastSibling();
        }

        if (activeLoadingPanel != null)
        {
            if (cataloguePanel != null) cataloguePanel.SetActive(false);
            if (detailsPanel != null) detailsPanel.SetActive(false);
            
            activeLoadingPanel.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("AppUIManager: Loading Panel Prefab is NOT assigned in the Inspector!");
        }
    }

    public void HideLoading()
    {
        if (activeLoadingPanel != null)
        {
            activeLoadingPanel.gameObject.SetActive(false);
        }
    }

    public void ClearAllPlacedObjects()
    {
        // Always try to clear both models and textures depending on what is active!
        // This ensures the "Clear All" button never fails silently.
        bool hasTextures = false;
        TexturePlacementManager texMgr = TexturePlacementManager.Instance;
        if (texMgr != null && texMgr.IsActive)
        {
            hasTextures = true;
        }

        bool isTexture = (SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode)
            || (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Texture"))
            || hasTextures;

        if (isTexture)
        {
            ClearTexturedPlanes();
            return;
        }

        // Deselect any selected object first
        if (ARStandaloneManipulator.SelectedObject != null)
        {
            ARStandaloneManipulator.SelectedObject.Deselect();
        }

        if (PlacedObjectsManager.Instance != null)
        {
            // Pool objects instead of destroying — they can be reused if same product is placed again
            PlacedObjectsManager.Instance.PoolAllObjects();
        }
        else
        {
            // Fallback: no manager, destroy directly
            ARStandaloneManipulator[] objects = FindObjectsOfType<ARStandaloneManipulator>();
            foreach (var obj in objects)
            {
                if (obj != null) Destroy(obj.transform.root.gameObject);
            }
        }

        // Reset placement state so user can tap to place again
        SurfacePlacementManager spm = FindObjectOfType<SurfacePlacementManager>();
        if (spm != null)
        {
            spm.OnEnterARMode();
        }
        
        Debug.Log("ClearAllPlacedObjects: Objects pooled for potential reuse.");
    }

    /// <summary>
    /// Clears all textures applied to AR planes and restores original materials.
    /// </summary>
    public void ClearTexturedPlanes()
    {
        TexturePlacementManager texMgr = TexturePlacementManager.Instance;
        if (texMgr != null)
        {
            texMgr.ClearTextures();
        }

        if (SelectedProductHolder.Instance != null)
        {
            SelectedProductHolder.Instance.ClearTexture();
        }

        // Refresh plane visibility based on the current grid toggle state
        TogglePlaneVisibility(arPlanesVisible);

        Debug.Log("ClearTexturedPlanes: All plane textures restored.");
    }

    private void ToggleAR(bool isAR)
    {
        // Globally enable/disable touch physics for 3D placed models
        ARStandaloneManipulator.GlobalARModeActive = isAR;

        if (planeManager != null)
        {
            planeManager.enabled = isAR;
            foreach (var plane in planeManager.trackables)
            {
                if (isAR)
                {
                    // Re-entering AR: activate the plane GameObject but respect the grid toggle
                    plane.gameObject.SetActive(true);
                    if (!arPlanesVisible)
                    {
                        // Grid is toggled off — apply visibility rules (hide horizontal, occlude vertical)
                        ApplyPlaneVisibility(plane, false);
                    }
                }
                else
                {
                    // Leaving AR: hide all planes
                    plane.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Fetches AR product data, downloads and caches the glTF model, and places the model in AR mode.
    /// Used by both ProductItemUI and ProductDetailsUI.
    /// </summary>
    public async void StartViewInARProcess(ProductData productData, Button triggeringButton)
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("AppUIManager: APIManager missing from scene!");
            return;
        }

        // ── Object Pool: try to reuse a pooled object for the same product ──
        if (PlacedObjectsManager.Instance != null)
        {
            // Evict pooled objects for OTHER products (free memory)
            PlacedObjectsManager.Instance.EvictPoolExcept(productData.id);

            // Check if we already have this product pooled
            GameObject pooled = PlacedObjectsManager.Instance.TryGetPooledObject(productData.id);
            if (pooled != null)
            {
                Debug.Log($"Reusing pooled AR object for '{productData.name}' (id={productData.id}). Skipping download.");
                
                // Re-set the selected product to the pooled object
                SelectedProductHolder.Instance.SetSelectedProduct(pooled);
                
                // Enter AR Mode directly — SurfacePlacementManager will handle placement
                ShowARMode();
                return;
            }
        }

        if (triggeringButton != null) triggeringButton.interactable = false;
        ShowLoading();
        
        Debug.Log($"AppUIManager: Initiating AR process for '{productData.name}'...");

        APIManager.Instance.GetARProductData(productData.id.ToString(), 
            onSuccess: async (arData) => 
            {
                if (!string.IsNullOrEmpty(arData.modelUrl))
                {
                    try 
                    {
                        // Extract filename from URL (ignoring S3 query parameters)
                        System.Uri uri = new System.Uri(arData.modelUrl);
                        string filename = System.IO.Path.GetFileName(uri.LocalPath);
                        string localPath = System.IO.Path.Combine(Application.persistentDataPath, filename);
                        string loadUrl = "";

                        if (System.IO.File.Exists(localPath))
                        {
                            Debug.Log($"Found cached model at {localPath}. Skipping download.");
                            loadUrl = "file://" + localPath;
                        }
                        else
                        {
                            Debug.Log($"Model not cached. Downloading from S3 and saving to {localPath}...");
                            
                            using (UnityWebRequest webRequest = UnityWebRequest.Get(arData.modelUrl))
                            {
                                webRequest.downloadHandler = new DownloadHandlerFile(localPath);
                                var operation = webRequest.SendWebRequest();
                                
                                ulong lastBytes = 0;
                                float lastTime = Time.time;
                                string speedText = "0 MB/s";

                                while (!operation.isDone)
                                {
                                    float progress = webRequest.downloadProgress;
                                    ulong downloadedBytes = webRequest.downloadedBytes;
                                    
                                    // Calculate speed every 0.25 seconds to avoid jitter
                                    if (Time.time - lastTime >= 0.25f)
                                    {
                                        ulong bytesSinceLast = downloadedBytes - lastBytes;
                                        float speedBps = bytesSinceLast / (Time.time - lastTime);
                                        float speedMbps = speedBps / (1024f * 1024f);
                                        speedText = speedMbps.ToString("F1") + " MB/s";
                                        
                                        lastBytes = downloadedBytes;
                                        lastTime = Time.time;
                                    }

                                    ulong totalBytes = 0;
                                    if (progress > 0)
                                    {
                                        totalBytes = (ulong)(downloadedBytes / progress);
                                    }
                                    
                                    if (activeLoadingPanel != null)
                                    {
                                        activeLoadingPanel.UpdateProgress(progress, downloadedBytes, totalBytes, speedText);
                                    }
                                    
                                    await System.Threading.Tasks.Task.Yield();
                                }

                                if (webRequest.result != UnityWebRequest.Result.Success)
                                {
                                    Debug.LogError($"Download failed: {webRequest.error}");
                                    if (System.IO.File.Exists(localPath)) System.IO.File.Delete(localPath);
                                    HideLoading();
                                    FallbackToHomepage();
                                    if (triggeringButton != null) triggeringButton.interactable = true;
                                    return;
                                }
                            }
                            
                            Debug.Log("Download complete and saved to cache!");
                            loadUrl = "file://" + localPath;
                        }

                        // Load the model from the local file
                        var gltf = new GLTFast.GltfImport();
                        bool success = await gltf.Load(loadUrl);
                        
                        if (success) {
                            GameObject downloadedModel = new GameObject("Cached_AR_Product_" + productData.id);
                            // Single scene architecture: spawn directly in the active scene
                            downloadedModel.SetActive(false); // Hide it for now
                            
                            success = await gltf.InstantiateMainSceneAsync(downloadedModel.transform);
                            
                            if (success) {
                                Debug.Log("Model fully downloaded and cached! Entering AR Scene...");
                                
                                // Pass the READY model to the holder
                                SelectedProductHolder.Instance.SetSelectedProduct(downloadedModel);
                                SelectedProductHolder.Instance.SetSelectedARData(arData);
                                
                                // Enter AR Mode
                                HideLoading();
                                ShowARMode();
                                
                                // Re-enable the button so they can place it again later!
                                if (triggeringButton != null) triggeringButton.interactable = true;
                            } else {
                                Debug.LogError("Failed to instantiate glTF scene.");
                                HideLoading();
                                ShowDetails(productData); // Keep details active since instantiate failed, but trigger homepage fallback
                                FallbackToHomepage();
                                if (triggeringButton != null) triggeringButton.interactable = true;
                            }
                        } else {
                            Debug.LogError($"Loading glTF failed from URL: {arData.modelUrl}");
                            HideLoading();
                            FallbackToHomepage();
                            if (triggeringButton != null) triggeringButton.interactable = true;
                        }
                    } 
                    catch (System.Exception e) 
                    {
                        Debug.LogError($"Exception during glTF load: {e.Message}");
                        HideLoading();
                        FallbackToHomepage();
                        if (triggeringButton != null) triggeringButton.interactable = true;
                    }
                }
                else
                {
                    Debug.LogError("API returned AR data, but modelUrl was empty!");
                    HideLoading();
                    FallbackToHomepage();
                    if (triggeringButton != null) triggeringButton.interactable = true;
                }
            }, 
            onError: (errorMsg) => 
            {
                Debug.LogError($"Failed to fetch AR data: {errorMsg}");
                HideLoading();
                FallbackToHomepage();
                if (triggeringButton != null) triggeringButton.interactable = true;
            });
    }

    /// <summary>
    /// Downloads a texture from the product's thumbnailUrl and enters AR texture mode.
    /// Used for flooring, wall paneling, and other texture products.
    /// </summary>
    public void StartTextureARProcess(ProductData productData, Button triggeringButton)
    {
        if (productData == null)
        {
            Debug.LogError("AppUIManager: StartTextureARProcess called with null productData!");
            return;
        }

        if (triggeringButton != null) triggeringButton.interactable = false;
        ShowLoading();

        Debug.Log($"AppUIManager: Starting texture AR for '{productData.name}'...");

        // Store product data
        if (SelectedProductHolder.Instance != null)
        {
            SelectedProductHolder.Instance.SetSelectedProductData(productData);
            SelectedProductHolder.Instance.isTextureMode = true;
        }

        // Download the texture image
        string textureUrl = productData.thumbnailUrl;
        if (string.IsNullOrEmpty(textureUrl))
        {
            Debug.LogError("AppUIManager: Product has no thumbnailUrl for texture!");
            HideLoading();
            FallbackToHomepage();
            if (triggeringButton != null) triggeringButton.interactable = true;
            return;
        }

        StartCoroutine(DownloadAndApplyTexture(textureUrl, productData, triggeringButton));
    }

    private IEnumerator DownloadAndApplyTexture(string url, ProductData productData, Button triggeringButton)
    {
        Sprite loadedSprite = null;
        yield return StartCoroutine(ImageCache.GetImage(productData.id, url, (sprite) => loadedSprite = sprite));

        if (loadedSprite != null && loadedSprite.texture != null)
        {
            Texture2D texture = loadedSprite.texture;
            Debug.Log($"AppUIManager: Successfully applied texture for '{productData.name}' ({texture.width}x{texture.height}).");
            SelectedProductHolder.Instance.SetSelectedTexture(texture);
            HideLoading();
            ShowARMode();
        }
        else
        {
            Debug.LogError($"AppUIManager: Failed to load texture for product {productData.name}");
            HideLoading();
            FallbackToHomepage();
            SelectedProductHolder.Instance.ClearTexture();
        }

        if (triggeringButton != null) triggeringButton.interactable = true;
    }

    private void FallbackToHomepage()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene("HomepageScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("HomepageScene");
        }
    }
}
