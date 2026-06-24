using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class SurfacePlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    private GameObject objectToPlace;
    public float floorHeightThreshold = 0.5f;

    [Header("Visual Feedback")]
    public GameObject placementIndicator;
    public UnityEngine.UI.Button placeButton; // Optional UI Button for manual placement
    public Material outlineMaterial; // Custom shader material for selection
    public TMP_FontAsset poppinsFont; // Poppins-Light SDF font asset

    [Header("Debug Display (assign Tutorial Text)")]
    public TextMeshProUGUI debugText; // Reuse tutorial text to show live status on device

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Pose lastValidPose;
    private bool hasValidPose = false;
    private bool objectPlaced = false;
    private bool isARModeActive = false;
    private float cachedLowestPlaneHeight = 0f;
    private bool isLowestPlaneHeightCached = false;

    private List<PlaneClassifications> allowedClassifications = new List<PlaneClassifications>();

    void Awake()
    {
        // Get references from THIS GameObject (XR Origin)
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();

        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }

        if (placeButton != null)
        {
            placeButton.onClick.AddListener(PlaceObjectOnCurrentSurface);
        }

        // Enable enhanced touch for reliable iOS input
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    public void OnEnterARMode()
    {
        objectPlaced = false;
        isARModeActive = true;

        if (raycastManager == null) raycastManager = GetComponent<ARRaycastManager>();
        if (planeManager == null) planeManager = GetComponent<ARPlaneManager>();

        if (planeManager != null)
        {
            planeManager.enabled = true;
        }

        // Get the tutorial text reliably through ARUIManager (which has it wired in the Inspector)
        debugText = null;
        ARUIManager arUI = FindObjectOfType<ARUIManager>();
        if (arUI != null)
        {
            debugText = arUI.tutorialText;
            // Make sure the panel is visible
            if (arUI.tutorialPanel != null)
                arUI.tutorialPanel.SetActive(true);
        }
        else
        {
            // Fallback: try to find by name (only works if active)
            GameObject tutPanel = GameObject.Find("TutorialText");
            if (tutPanel != null)
            {
                debugText = tutPanel.GetComponent<TextMeshProUGUI>();
            }
        }

        bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;

        if (SelectedProductHolder.Instance != null)
        {
            objectToPlace = SelectedProductHolder.Instance.GetSelectedProduct();

            if (objectToPlace == null && !isTextureMode)
            {
                SetDebug("Something went wrong. Please go back and try again.");
                return;
            }

            allowedClassifications.Clear();

            if (isTextureMode)
            {
                ProductData productData = SelectedProductHolder.Instance.GetSelectedProductData();
                if (productData != null)
                {
                    if (productData.IsWallTexture)
                    {
                        allowedClassifications.Add(PlaneClassifications.WallFace);
                    }
                    if (productData.IsFloorTexture)
                    {
                        allowedClassifications.Add(PlaneClassifications.Floor);
                    }
                    if (allowedClassifications.Count == 0)
                    {
                        allowedClassifications.Add(PlaneClassifications.Floor);
                    }
                }
            }
            else
            {
                // 1. Try to get classifications from the backend AR data
                ARProductData arData = SelectedProductHolder.Instance.GetSelectedARData();
                if (arData != null && arData.arPlacement != null && arData.arPlacement.allowedPlaneClassifications != null)
                {
                    foreach (string classStr in arData.arPlacement.allowedPlaneClassifications)
                    {
                        if (System.Enum.TryParse(classStr, true, out PlaneClassifications parsed))
                        {
                            allowedClassifications.Add(parsed);
                        }
                    }
                }

                // 2. Fallback: if backend didn't provide classifications, infer from category/product name
                if (allowedClassifications.Count == 0)
                {
                    ProductData productData = SelectedProductHolder.Instance.GetSelectedProductData();
                    allowedClassifications = GetClassificationsForProduct(productData);
                }

                // 3. Ultimate fallback: Floor
                if (allowedClassifications.Count == 0)
                {
                    allowedClassifications.Add(PlaneClassifications.Floor);
                }
            }

            // Show context-aware scanning instructions
            SetDebug(GetScanInstructionText());
        }
        else
        {
            SetDebug("Something went wrong. Please go back and try again.");
        }
    }

    /// <summary>
    /// Smart fallback: maps product category and name to appropriate surface types.
    /// </summary>
    private List<PlaneClassifications> GetClassificationsForProduct(ProductData data)
    {
        var classifications = new List<PlaneClassifications>();
        if (data == null) return classifications;

        string category = (data.categoryName ?? "").ToLower();
        string productName = (data.name ?? "").ToLower();

        // Ceiling products: chandeliers, pendant lights, ceiling fans
        if (productName.Contains("pendant") || productName.Contains("chandelier") || 
            productName.Contains("ceiling") || productName.Contains("hanging"))
        {
            classifications.Add(PlaneClassifications.Ceiling);
            return classifications;
        }

        // Wall products: wall art, mirrors, shelves (wall-mounted), clocks, frames
        if (productName.Contains("wall") || productName.Contains("mirror") || 
            productName.Contains("frame") || productName.Contains("clock") ||
            productName.Contains("painting") || productName.Contains("sconce"))
        {
            classifications.Add(PlaneClassifications.WallFace);
            return classifications;
        }

        // Category-based mapping
        switch (category)
        {
            case "living room":
            case "bedroom":
            case "dining room":
                // Sofas, chairs, beds, tables → Floor only
                classifications.Add(PlaneClassifications.Floor);
                break;

            case "decor":
                // Decor items like vases, bookshelves, cabinets → Floor + Table
                classifications.Add(PlaneClassifications.Floor);
                classifications.Add(PlaneClassifications.Table);
                break;

            case "outdoor":
                classifications.Add(PlaneClassifications.Floor);
                break;

            default:
                // Unknown category → Floor as safest default
                classifications.Add(PlaneClassifications.Floor);
                break;
        }

        return classifications;
    }

    /// <summary>
    /// Returns a user-friendly instruction based on what surfaces we're looking for.
    /// </summary>
    private string GetScanInstructionText()
    {
        bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;
        bool wantsFloor = allowedClassifications.Contains(PlaneClassifications.Floor);
        bool wantsWall = allowedClassifications.Contains(PlaneClassifications.WallFace);
        bool wantsCeiling = allowedClassifications.Contains(PlaneClassifications.Ceiling);
        bool wantsTable = allowedClassifications.Contains(PlaneClassifications.Table);

        if (isTextureMode)
        {
            if (wantsWall && !wantsFloor)
                return "Point your camera at a wall to apply the texture.\n(Tip: If it disappears as the plane updates, tap or click Place to re-apply)";
            if (wantsFloor && !wantsWall)
                return "Scan the floor to apply the flooring texture.\n(Tip: If it disappears as the plane updates, tap or click Place to re-apply)";
            return "Scan a surface to apply the texture.\n(Tip: If it disappears as the plane updates, tap or click Place to re-apply)";
        }

        if (wantsCeiling)
            return "Point your camera at the ceiling to place this item...";
        if (wantsWall)
            return "Point your camera at a wall to place this item...";
        if (wantsTable && !wantsFloor)
            return "Point your camera at a table surface to place this item...";
        if (wantsFloor && wantsTable)
            return "Scan the floor or a table surface to place this item...";
        
        return "Slowly move your camera around to scan the room...";
    }

    void OnDestroy()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Invalidate cached lowest plane height when trackable changes occur
        isLowestPlaneHeightCached = false;

        foreach (var plane in args.added)
        {
            Debug.Log($"Plane added: {GetPlaneDebugInfo(plane)}");
        }
    }

    void Update()
    {
        if (!isARModeActive) return;

        UpdatePlacementPreview();
        HandlePlacementInput();
    }

    public void OnExitARMode()
    {
        isARModeActive = false;
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }
        if (debugText != null && debugText.gameObject.activeInHierarchy)
        {
            debugText.gameObject.SetActive(false);
            if (debugText.transform.parent != null)
            {
                debugText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    void UpdatePlacementPreview()
    {
        if (objectPlaced)
        {
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
            return;
        }

        if (raycastManager == null)
        {
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;

            if (hitPlane != null && IsValidSurface(hitPlane))
            {
                lastValidPose = hits[0].pose;
                hasValidPose = true;

                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(true);
                    placementIndicator.transform.SetPositionAndRotation(
                        lastValidPose.position,
                        lastValidPose.rotation
                    );
                }

                // Show user-friendly placement instruction
                if (!objectPlaced)
                {
                    bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;
                    if (isTextureMode)
                    {
                        SetDebug("Surface detected! Tap to apply the texture.");
                    }
                    else
                    {
                        SetDebug("Surface detected! Tap to place your furniture.");
                    }
                }
            }
            else
            {
                if (placementIndicator != null)
                    placementIndicator.SetActive(false);
                hasValidPose = false;
            }
        }
        else
        {
            if (placementIndicator != null)
                placementIndicator.SetActive(false);
            hasValidPose = false;
        }
    }

    void HandlePlacementInput()
    {
        if (objectPlaced)
            return;

        bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;
        if (objectToPlace == null && !isTextureMode)
            return;

        Vector2 inputPosition = Vector2.zero;
        bool inputDetected = false;

        // Touch (Device) — Using EnhancedTouch for reliable iOS support
        var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (activeTouches.Count > 0)
        {
            var touch = activeTouches[0];
            if (activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Check if the touch is over a UI element (e.g., Show Grid toggle, Delete button)
                if (IsTouchOverUI(touch.screenPosition))
                {
                    return;
                }

                inputPosition = touch.screenPosition;
                
                // Check if tapped on an existing object to avoid double-spawning
                Ray ray = Camera.main.ScreenPointToRay(inputPosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.GetComponentInParent<ARStandaloneManipulator>() != null)
                    {
                        inputDetected = false; // Let the manipulator handle selection
                        return;
                    }
                }
                
                inputDetected = true;
                Debug.Log($"Touch detected at: {inputPosition}");
            }
        }
        // Mouse (Editor)
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            // Check if mouse is over a UI button/toggle
            if (IsTouchOverUI(mousePos))
                return;

            inputPosition = mousePos;
            inputDetected = true;
        }

        if (!inputDetected)
            return;

        if (raycastManager == null)
        {
            return;
        }

        if (raycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;

            if (hitPlane == null)
            {
                return;
            }

            if (!IsValidSurface(hitPlane))
            {
                if (isTextureMode)
                    SetDebug("Aim at a valid floor or wall surface to apply the texture.");
                else
                    SetDebug("Please aim at a valid surface for this product.");
                return;
            }

            if (isTextureMode)
            {
                ApplyTextureToPlane(hitPlane);
            }
            else
            {
                PerformPlacement(hitPose);
            }
        }
        else
        {
            if (isTextureMode)
                SetDebug("Keep scanning - point camera at floor or wall to apply texture.");
            else
                SetDebug("Keep scanning - move your camera slowly around the room.");
        }
    }

    public void PlaceObjectOnCurrentSurface()
    {
        bool isTextureMode = SelectedProductHolder.Instance != null && SelectedProductHolder.Instance.isTextureMode;
        if (!isTextureMode && (objectPlaced || objectToPlace == null)) return;

        if (isTextureMode)
        {
            if (!hasValidPose)
            {
                SetDebug("Cannot apply texture: No valid surface detected yet. Keep scanning.");
                return;
            }

            // Raycast to find the plane at screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (raycastManager != null && raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;
                if (hitPlane != null && IsValidSurface(hitPlane))
                {
                    ApplyTextureToPlane(hitPlane);
                }
                else
                {
                    SetDebug("Aim at a valid floor or wall surface to apply the texture.");
                }
            }
            return;
        }

        if (!hasValidPose)
        {
            SetDebug("Cannot place object: No valid surface detected yet. Keep scanning.");
            return;
        }

        PerformPlacement(lastValidPose);
    }

    private void PerformPlacement(Pose hitPose)
    {
        // ---- PLACE THE OBJECT ----
        objectToPlace.SetActive(true);

        // Determine if this is a reused (pooled) object or a fresh one
        bool isReused = objectToPlace.GetComponent<ARStandaloneManipulator>() != null;

        if (!isReused)
        {
            // Only normalize scale for fresh objects — pooled ones retain their last scale
            NormalizeModelScale(objectToPlace);
        }

        float bottomOffset = AdjustPivotAndCollider(objectToPlace, hitPose);

        // Put everything on the SAME object that has the BoxCollider (objectToPlace / the parent)
        ARStandaloneManipulator manipulator = objectToPlace.GetComponent<ARStandaloneManipulator>();
        if (manipulator == null)
        {
            manipulator = objectToPlace.AddComponent<ARStandaloneManipulator>();
        }
        manipulator.outlineMaterial = this.outlineMaterial;
        manipulator.poppinsFont = this.poppinsFont;
        manipulator.bottomOffset = bottomOffset; // So dragging keeps the object on the floor

        // Tell the manipulator if this is a floor item so it can lock Y-axis movement
        manipulator.isFloorPlacement = allowedClassifications.Contains(PlaneClassifications.Floor);

        // Rigidbody is needed for Physics.RaycastAll to register the BoxCollider
        Rigidbody rb = objectToPlace.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = objectToPlace.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;

        // Force the object to face the camera upon spawning
        Vector3 lookPos = Camera.main.transform.position - objectToPlace.transform.position;
        lookPos.y = 0; // Keep it flat on the ground
        if (lookPos != Vector3.zero)
        {
            objectToPlace.transform.rotation = Quaternion.LookRotation(-lookPos);
        }

#if !UNITY_EDITOR
        if (objectToPlace.GetComponent<ARAnchor>() == null)
        {
            objectToPlace.AddComponent<ARAnchor>();
        }
#endif

        // Extract product ID for pool tracking
        int productId = -1;
        if (SelectedProductHolder.Instance != null)
        {
            ProductData pData = SelectedProductHolder.Instance.GetSelectedProductData();
            if (pData != null) productId = pData.id;
        }

        if (PlacedObjectsManager.Instance != null)
        {
            PlacedObjectsManager.Instance.RegisterPlacedObject(objectToPlace, productId);
        }

        objectPlaced = true;

        if (placementIndicator != null)
            placementIndicator.SetActive(false);

        SetDebug("Furniture placed!\n\nTap to select  |  Drag to move\nPinch to scale  |  Twist to rotate");
        Debug.Log($"Product placed successfully. (Reused: {isReused}, ProductID: {productId})");
    }

    /// <summary>
    /// Checks if a screen position is over an interactive UI element (Button or Toggle).
    /// Uses manual raycast instead of IsPointerOverGameObject which is unreliable with New Input System.
    /// </summary>
    private bool IsTouchOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Only block if we hit an actual interactive UI element, not background panels
            if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null ||
                result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                result.gameObject.GetComponentInParent<UnityEngine.UI.Toggle>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void SetDebug(string msg)
    {
        Debug.Log($"[AR Debug] {msg}");
        if (debugText != null)
        {
            debugText.text = msg;
            debugText.gameObject.SetActive(true);
            // Also make parent panel visible
            if (debugText.transform.parent != null)
                debugText.transform.parent.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Adjusts the object so its bottom sits on the surface and adds a BoxCollider.
    /// Returns the Y offset from the surface to the object's pivot (used during drag to keep feet on floor).
    /// </summary>
    private float AdjustPivotAndCollider(GameObject obj, Pose hitPose)
    {
        obj.transform.position = hitPose.position;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        float bottomOffset = 0f;

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            float bottomY = bounds.min.y;
            float offset = hitPose.position.y - bottomY;
            obj.transform.position += new Vector3(0, offset, 0);
            bottomOffset = offset; // Store for use during dragging

            // Re-calculate bounds after moving
            bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            // Create ONE giant bulletproof BoxCollider on the parent to catch ALL touches
            BoxCollider bc = obj.GetComponent<BoxCollider>();
            if (bc == null) bc = obj.AddComponent<BoxCollider>();
            
            bc.center = obj.transform.InverseTransformPoint(bounds.center);
            bc.size = obj.transform.InverseTransformVector(bounds.size);
        }
        else
        {
            // Fallback if no meshes exist
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }
        }

        return bottomOffset;
    }

    /// <summary>
    /// Normalizes the model scale so it appears at a reasonable real-world size.
    /// Uses product dimensions from the API if available, otherwise estimates
    /// a target size based on the product category.
    /// </summary>
    private void NormalizeModelScale(GameObject obj)
    {
        // Temporarily set scale to 1 to measure the raw model size
        obj.transform.localScale = Vector3.one;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        float currentHeight = bounds.size.y;
        float currentWidth = bounds.size.x;
        float currentDepth = bounds.size.z;
        float currentMax = Mathf.Max(currentHeight, currentWidth, currentDepth);

        if (currentMax <= 0.001f) return; // Avoid division by zero

        // Determine target size
        float targetHeight = 0.8f; // Default: 0.8 meters tall

        // Try to use actual dimensions from the API
        ProductData productData = SelectedProductHolder.Instance?.GetSelectedProductData();
        if (productData != null && productData.dimensions != null && productData.dimensions.height > 0)
        {
            // API dimensions are in cm, convert to meters
            targetHeight = productData.dimensions.height / 100f;
            Debug.Log($"Using API dimensions for scaling: {productData.dimensions.height}cm -> {targetHeight}m");
        }
        else
        {
            // Estimate target height based on category
            string category = (productData?.categoryName ?? "").ToLower();
            string productName = (productData?.name ?? "").ToLower();

            if (productName.Contains("sofa") || productName.Contains("couch"))
                targetHeight = 0.75f; // ~75cm
            else if (productName.Contains("chair") || productName.Contains("armchair"))
                targetHeight = 0.80f; // ~80cm
            else if (productName.Contains("table") && (productName.Contains("coffee") || productName.Contains("side") || productName.Contains("center")))
                targetHeight = 0.40f; // ~40cm coffee/side tables
            else if (productName.Contains("table"))
                targetHeight = 0.70f; // ~70cm dining tables
            else if (productName.Contains("bed"))
                targetHeight = 0.45f; // ~45cm bed height
            else if (productName.Contains("bookshelf") || productName.Contains("cabinet") || productName.Contains("shelf"))
                targetHeight = 1.50f; // ~150cm
            else if (productName.Contains("lamp") || productName.Contains("light"))
                targetHeight = 0.40f; // ~40cm
            else if (productName.Contains("pendant") || productName.Contains("chandelier"))
                targetHeight = 0.50f; // ~50cm
            else if (category == "living room")
                targetHeight = 0.75f;
            else if (category == "bedroom")
                targetHeight = 0.50f;
            else if (category == "dining room")
                targetHeight = 0.70f;
            else if (category == "decor")
                targetHeight = 0.60f;

            Debug.Log($"Using estimated target height for '{productData?.name}': {targetHeight}m (model raw height: {currentHeight}m)");
        }

        // Calculate scale factor based on the model's height
        float scaleFactor = targetHeight / currentHeight;

        // Safety clamp: don't let the scale go insanely large or small
        scaleFactor = Mathf.Clamp(scaleFactor, 0.001f, 50f);

        obj.transform.localScale = Vector3.one * scaleFactor;
        Debug.Log($"Model scaled by {scaleFactor:F4}x (from {currentHeight:F2}m to {targetHeight:F2}m target height)");
    }

    bool IsValidSurface(ARPlane plane)
    {
        var classifications = plane.classifications;

        if (classifications != PlaneClassifications.None)
        {
            foreach (var allowed in allowedClassifications)
            {
                if ((classifications & allowed) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        // Fallback for unclassified planes
        if (allowedClassifications.Contains(PlaneClassifications.WallFace))
        {
            if (plane.alignment == PlaneAlignment.Vertical) return true;
        }

        if (allowedClassifications.Contains(PlaneClassifications.Floor))
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp)
            {
                float currentPlaneHeight = plane.center.y;
                float lowestPlaneHeight = GetLowestPlaneHeight();
                return Mathf.Abs(currentPlaneHeight - lowestPlaneHeight) < floorHeightThreshold;
            }
        }

        if (allowedClassifications.Contains(PlaneClassifications.Table) || allowedClassifications.Contains(PlaneClassifications.Seat))
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp)
            {
                float currentPlaneHeight = plane.center.y;
                float lowestPlaneHeight = GetLowestPlaneHeight();
                return (currentPlaneHeight - lowestPlaneHeight) > floorHeightThreshold;
            }
        }

        if (allowedClassifications.Contains(PlaneClassifications.Ceiling))
        {
            if (plane.alignment == PlaneAlignment.HorizontalDown) return true;
        }

        return false;
    }

    float GetLowestPlaneHeight()
    {
        if (isLowestPlaneHeightCached)
        {
            return cachedLowestPlaneHeight;
        }

        float lowest = float.MaxValue;

        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    if (plane.center.y < lowest)
                    {
                        lowest = plane.center.y;
                    }
                }
            }
        }

        cachedLowestPlaneHeight = lowest == float.MaxValue ? 0f : lowest;
        isLowestPlaneHeightCached = true;
        return cachedLowestPlaneHeight;
    }

    string GetPlaneDebugInfo(ARPlane plane)
    {
        var classifications = plane.classifications;
        string info = $"Alignment: {plane.alignment}, Height: {plane.center.y:F2}m";

        if (classifications != PlaneClassifications.None)
        {
            info += $", Classification: {GetClassificationNames(classifications)}";
        }
        else
        {
            info += ", Classification: None (height-based)";
        }

        return info;
    }

    string GetClassificationNames(PlaneClassifications classifications)
    {
        List<string> names = new List<string>();

        if ((classifications & PlaneClassifications.Floor) != 0) names.Add("Floor");
        if ((classifications & PlaneClassifications.Ceiling) != 0) names.Add("Ceiling");
        if ((classifications & PlaneClassifications.WallFace) != 0) names.Add("Wall");
        if ((classifications & PlaneClassifications.Table) != 0) names.Add("Table");
        if ((classifications & PlaneClassifications.Seat) != 0) names.Add("Seat");
        if ((classifications & PlaneClassifications.Couch) != 0) names.Add("Couch");
        if ((classifications & PlaneClassifications.Other) != 0) names.Add("Other");

        return names.Count > 0 ? string.Join(", ", names) : "None";
    }

    private void ApplyTextureToPlane(ARPlane plane)
    {
        if (plane == null) return;

        TexturePlacementManager texMgr = TexturePlacementManager.Instance;
        if (texMgr == null)
        {
            texMgr = FindObjectOfType<TexturePlacementManager>();
        }

        if (texMgr != null)
        {
            if (!texMgr.IsActive)
            {
                Texture2D tex = SelectedProductHolder.Instance.GetSelectedTexture();
                ProductData pd = SelectedProductHolder.Instance.GetSelectedProductData();
                if (tex != null)
                {
                    texMgr.Activate(tex, pd);
                }
            }

            texMgr.ApplyToPlane(plane);
            SetDebug("Applied texture to the selected surface!");
        }
        else
        {
            Debug.LogError("ApplyTextureToPlane: TexturePlacementManager not found!");
        }
    }
}