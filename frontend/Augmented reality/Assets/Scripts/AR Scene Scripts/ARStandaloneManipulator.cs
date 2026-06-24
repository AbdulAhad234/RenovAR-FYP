using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARStandaloneManipulator : MonoBehaviour
{
    public static ARStandaloneManipulator SelectedObject { get; private set; }

    // Global flag controlled by AppUIManager to disable physics/touch when in 2D UI
    public static bool GlobalARModeActive = true;

    public Material outlineMaterial;
    public TMPro.TMP_FontAsset poppinsFont;
    [HideInInspector] public float bottomOffset = 0f; // Y offset to keep object sitting on the floor
    [HideInInspector] public bool isFloorPlacement = true; // If true, Y movement is locked during drag

    private float placedY; // The Y position when the object was placed (used for floor-locking)
    
    private List<Renderer> renderers = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    private ARRaycastManager raycastManager;
    private bool isSelected = false;

    // Gesture tracking
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float initialTwistAngle;
    private Quaternion initialRotation;
    
    private Vector2 dragStartPos;
    private bool hasDragStarted;

    private TMPro.TextMeshProUGUI debugText;

    // ── Bounding Box Visualizer ────────────────────────────────────
    private BoundingBoxVisualizer boundingBox;
    private bool wasScaling  = false;
    private bool wasRotating = false;

    // Scale snap-lock: prevents scaling past optimal until a new pinch gesture
    private bool hasSnappedThisGesture = false;
    private Vector3 snappedScale;

    // Cached references (avoid per-frame FindObjectOfType)
    private ARPlaneManager cachedPlaneManager;
    private List<ARRaycastHit> reusableHitList = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
        
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends)
        {
            if (r != null)
            {
                renderers.Add(r);
                originalMaterials[r] = r.materials;
            }
        }
    }

    void Start()
    {
        var spm = FindObjectOfType<SurfacePlacementManager>();
        if (spm != null) debugText = spm.debugText;

        // Record the placed Y position for floor-locking
        placedY = transform.position.y;

        // Create and configure the bounding box visualizer
        EnsureBoundingBox();
    }

    /// <summary>
    /// Creates the BoundingBoxVisualizer component if not already present
    /// and configures optimal dimensions from product data.
    /// </summary>
    private void EnsureBoundingBox()
    {
        boundingBox = GetComponent<BoundingBoxVisualizer>();
        if (boundingBox == null)
        {
            boundingBox = gameObject.AddComponent<BoundingBoxVisualizer>();
        }
        boundingBox.fontAsset = this.poppinsFont;

        // Set optimal dimensions from ProductData if available
        if (SelectedProductHolder.Instance != null)
        {
            ProductData pData = SelectedProductHolder.Instance.GetSelectedProductData();
            if (pData != null && pData.dimensions != null && pData.dimensions.height > 0)
            {
                // API dimensions are in cm, convert to metres
                if (pData.dimensions.width > 0)
                    boundingBox.optimalWidth = pData.dimensions.width / 100f;
                if (pData.dimensions.height > 0)
                    boundingBox.optimalHeight = pData.dimensions.height / 100f;
                if (pData.dimensions.depth > 0)
                    boundingBox.optimalDepth = pData.dimensions.depth / 100f;

                Debug.Log($"BoundingBox optimal dims set from API: {pData.dimensions.width}W × {pData.dimensions.height}H × {pData.dimensions.depth}D cm");
            }
            else if (pData != null)
            {
                // Fallback: estimate optimal dimensions from product name/category
                EstimateOptimalDimensions(pData);
            }
        }
    }

    /// <summary>
    /// Estimates optimal real-world dimensions (in metres) for known furniture types
    /// when the API doesn't provide dimensions.
    /// </summary>
    private void EstimateOptimalDimensions(ProductData pData)
    {
        string name = (pData.name ?? "").ToLower();
        string category = (pData.categoryName ?? "").ToLower();

        float w = -1f, h = -1f, d = -1f; // metres

        // ── Seating ─────────────────────────────────────────
        if (name.Contains("sofa") || name.Contains("couch"))
        { w = 2.00f; h = 0.85f; d = 0.90f; }
        else if (name.Contains("loveseat"))
        { w = 1.50f; h = 0.85f; d = 0.85f; }
        else if (name.Contains("sectional"))
        { w = 2.80f; h = 0.85f; d = 1.00f; }
        else if (name.Contains("armchair") || name.Contains("accent chair"))
        { w = 0.80f; h = 0.85f; d = 0.80f; }
        else if (name.Contains("recliner"))
        { w = 0.85f; h = 1.00f; d = 0.90f; }
        else if (name.Contains("office chair") || name.Contains("desk chair"))
        { w = 0.65f; h = 1.10f; d = 0.65f; }
        else if (name.Contains("dining chair"))
        { w = 0.45f; h = 0.90f; d = 0.50f; }
        else if (name.Contains("stool") || name.Contains("bar stool"))
        { w = 0.40f; h = 0.75f; d = 0.40f; }
        else if (name.Contains("chair"))
        { w = 0.55f; h = 0.85f; d = 0.55f; }
        else if (name.Contains("bean bag") || name.Contains("beanbag"))
        { w = 0.80f; h = 0.70f; d = 0.80f; }
        else if (name.Contains("ottoman") || name.Contains("pouf") || name.Contains("footrest"))
        { w = 0.50f; h = 0.40f; d = 0.50f; }
        else if (name.Contains("bench"))
        { w = 1.20f; h = 0.45f; d = 0.40f; }

        // ── Tables ──────────────────────────────────────────
        else if (name.Contains("coffee table") || name.Contains("center table"))
        { w = 1.10f; h = 0.45f; d = 0.60f; }
        else if (name.Contains("side table") || name.Contains("end table") || name.Contains("accent table"))
        { w = 0.50f; h = 0.55f; d = 0.50f; }
        else if (name.Contains("dining table"))
        { w = 1.60f; h = 0.75f; d = 0.90f; }
        else if (name.Contains("console table") || name.Contains("console"))
        { w = 1.20f; h = 0.80f; d = 0.35f; }
        else if (name.Contains("desk") || name.Contains("study table") || name.Contains("work table"))
        { w = 1.20f; h = 0.75f; d = 0.60f; }
        else if (name.Contains("nightstand") || name.Contains("bedside"))
        { w = 0.45f; h = 0.55f; d = 0.40f; }
        else if (name.Contains("table"))
        { w = 1.20f; h = 0.75f; d = 0.70f; }

        // ── Beds ────────────────────────────────────────────
        else if (name.Contains("king bed") || name.Contains("king size"))
        { w = 2.00f; h = 0.50f; d = 2.10f; }
        else if (name.Contains("queen bed") || name.Contains("queen size"))
        { w = 1.60f; h = 0.50f; d = 2.00f; }
        else if (name.Contains("single bed") || name.Contains("twin bed"))
        { w = 1.00f; h = 0.50f; d = 2.00f; }
        else if (name.Contains("bunk bed"))
        { w = 1.00f; h = 1.70f; d = 2.00f; }
        else if (name.Contains("bed"))
        { w = 1.50f; h = 0.50f; d = 2.00f; }

        // ── Storage ─────────────────────────────────────────
        else if (name.Contains("wardrobe") || name.Contains("closet") || name.Contains("almari"))
        { w = 1.50f; h = 2.00f; d = 0.60f; }
        else if (name.Contains("bookshelf") || name.Contains("bookcase"))
        { w = 0.80f; h = 1.80f; d = 0.35f; }
        else if (name.Contains("shelf") || name.Contains("shelving"))
        { w = 0.90f; h = 1.50f; d = 0.35f; }
        else if (name.Contains("cabinet") || name.Contains("cupboard"))
        { w = 0.80f; h = 1.20f; d = 0.45f; }
        else if (name.Contains("dresser") || name.Contains("drawer") || name.Contains("chest"))
        { w = 1.00f; h = 0.80f; d = 0.45f; }
        else if (name.Contains("tv stand") || name.Contains("tv unit") || name.Contains("media") || name.Contains("entertainment"))
        { w = 1.50f; h = 0.55f; d = 0.40f; }
        else if (name.Contains("shoe rack") || name.Contains("shoe cabinet"))
        { w = 0.80f; h = 0.90f; d = 0.35f; }

        // ── Lighting ────────────────────────────────────────
        else if (name.Contains("floor lamp") || name.Contains("standing lamp"))
        { w = 0.35f; h = 1.50f; d = 0.35f; }
        else if (name.Contains("table lamp") || name.Contains("desk lamp"))
        { w = 0.25f; h = 0.45f; d = 0.25f; }
        else if (name.Contains("chandelier"))
        { w = 0.70f; h = 0.60f; d = 0.70f; }
        else if (name.Contains("pendant") || name.Contains("hanging light"))
        { w = 0.40f; h = 0.35f; d = 0.40f; }
        else if (name.Contains("lamp") || name.Contains("light"))
        { w = 0.30f; h = 0.45f; d = 0.30f; }

        // ── Decor ───────────────────────────────────────────
        else if (name.Contains("mirror"))
        { w = 0.60f; h = 0.90f; d = 0.05f; }
        else if (name.Contains("clock"))
        { w = 0.35f; h = 0.35f; d = 0.05f; }
        else if (name.Contains("vase"))
        { w = 0.20f; h = 0.35f; d = 0.20f; }
        else if (name.Contains("rug") || name.Contains("carpet"))
        { w = 2.00f; h = 0.02f; d = 1.50f; }
        else if (name.Contains("painting") || name.Contains("frame") || name.Contains("wall art"))
        { w = 0.60f; h = 0.80f; d = 0.04f; }
        else if (name.Contains("plant") || name.Contains("planter"))
        { w = 0.30f; h = 0.50f; d = 0.30f; }
        else if (name.Contains("curtain") || name.Contains("drape"))
        { w = 1.50f; h = 2.40f; d = 0.10f; }

        // ── Category-based fallback ─────────────────────────
        else if (category == "living room")
        { w = 1.50f; h = 0.80f; d = 0.80f; }
        else if (category == "bedroom")
        { w = 1.50f; h = 0.50f; d = 2.00f; }
        else if (category == "dining room")
        { w = 1.40f; h = 0.75f; d = 0.80f; }
        else if (category == "decor")
        { w = 0.40f; h = 0.60f; d = 0.40f; }
        else if (category == "outdoor")
        { w = 0.70f; h = 0.85f; d = 0.70f; }

        // ── Ultimate fallback ───────────────────────────────
        else
        { w = 0.80f; h = 0.80f; d = 0.80f; }

        if (boundingBox != null)
        {
            boundingBox.optimalWidth  = w;
            boundingBox.optimalHeight = h;
            boundingBox.optimalDepth  = d;
            Debug.Log($"BoundingBox optimal dims estimated for '{pData.name}': {w*100:F0}W × {h*100:F0}H × {d*100:F0}D cm");
        }
    }

    private int tapCounter = 0;
    private bool isInMoveMode = false;
    
    // Double-tap detection
    private float lastTapTime = 0f;
    private float doubleTapThreshold = 0.35f; // seconds between taps to count as double-tap

    void Update()
    {
        if (!GlobalARModeActive) return; // Completely freeze physics and touch detection when UI is open

        bool isTapped = false;
        Vector2 touchPos = Vector2.zero;

        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == TouchPhase.Began)
        {
            if (!IsTouchOverUI(Touch.activeTouches[0].screenPosition))
            {
                isTapped = true;
                touchPos = Touch.activeTouches[0].screenPosition;
            }
        }
        else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            if (!IsTouchOverUI(mousePos))
            {
                isTapped = true;
                touchPos = mousePos;
            }
        }

        if (isTapped)
        {
            tapCounter++;
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            
            RaycastHit[] hits = Physics.RaycastAll(ray);
            Debug.Log($"[ARStandaloneManipulator] Tap registered at {touchPos}. Physics.RaycastAll returned {hits.Length} hits.");
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                Debug.Log($"  Hit[{i}]: name='{h.transform.name}', IsChildOfUs={h.transform.IsChildOf(transform)}, Point={h.point}, Dist={h.distance}");
            }

            bool hitOurObject = false;

            // Check if we tapped on one of our action buttons first
            foreach (var hit in hits)
            {
                if (boundingBox != null)
                {
                    if (hit.transform.gameObject == boundingBox.OptimalButton)
                    {
                        Debug.Log("[ARStandaloneManipulator] Hit Optimal Button! Triggering ScaleToOptimal.");
                        ScaleToOptimal();
                        return;
                    }
                    else if (hit.transform.gameObject == boundingBox.DeleteButton)
                    {
                        Debug.Log("[ARStandaloneManipulator] Hit Delete Button! Triggering DeleteObject.");
                        DeleteObject();
                        return;
                    }
                }
            }

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                {
                    hitOurObject = true;
                }
            }

            if (hitOurObject)
            {
                float timeSinceLastTap = Time.time - lastTapTime;
                lastTapTime = Time.time;

                if (isSelected && timeSinceLastTap < doubleTapThreshold)
                {
                    // Double-tap on selected object → toggle move mode
                    isInMoveMode = !isInMoveMode;
                    if (isInMoveMode)
                    {
                        if (debugText != null) debugText.text = "Move mode! Drag to reposition.\nDouble-tap again to exit move mode.";
                    }
                    else
                    {
                        if (debugText != null) debugText.text = "Scale/Rotate mode.\nPinch to scale | Twist to rotate\nDouble-tap to move.";
                    }
                }
                else if (!isSelected)
                {
                    // First tap → select (scale/rotate mode)
                    Select();
                    isInMoveMode = false;
                    if (debugText != null) debugText.text = "Selected! Pinch to scale | Twist to rotate\nDouble-tap to move.";
                }
                return;
            }
            else if (SelectedObject == this)
            {
                Deselect();
                if (debugText != null) debugText.text = "Tap on furniture to select it.";
            }
        }

        if (!isSelected) return;

        // -- TRANSLATION (Moving) — Only when in move mode --
        if (isInMoveMode)
        {
            HandleMovement();
        }
        else
        {
            // -- SCALING & ROTATING — Only when NOT in move mode --
            HandleScaleRotate();
        }
    }

    private void HandleMovement()
    {
        bool isDragging = false;
        Vector2 dragPos = Vector2.zero;
        float dragThreshold = 10f;

        // Phone Drag
        if (Touch.activeTouches.Count == 1)
        {
            Touch touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Began)
            {
                dragStartPos = touch.screenPosition;
                hasDragStarted = false;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (!hasDragStarted && Vector2.Distance(touch.screenPosition, dragStartPos) > dragThreshold)
                {
                    hasDragStarted = true;
                }
                if (hasDragStarted)
                {
                    isDragging = true;
                    dragPos = touch.screenPosition;
                }
            }
        }
        // Editor Mouse Drag (Left Click Hold)
        else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            isDragging = true;
            dragPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }

        if (isDragging)
        {
            reusableHitList.Clear();
            if (raycastManager != null && raycastManager.Raycast(dragPos, reusableHitList, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = reusableHitList[0].pose;
                Vector3 pos = hitPose.position;

                Vector3 surfaceNormal = hitPose.rotation * Vector3.up;
                pos += surfaceNormal * bottomOffset;

                if (isFloorPlacement)
                {
                    pos.y = placedY;
                }

                if (isFloorPlacement)
                {
                    pos = ClampPositionToWalls(pos);
                }

                transform.position = pos;

                if (!isFloorPlacement && Mathf.Abs(surfaceNormal.y) < 0.5f)
                {
                    transform.rotation = Quaternion.LookRotation(surfaceNormal);
                }
            }
            else if (Application.isEditor)
            {
                Plane groundPlane = new Plane(Vector3.up, transform.position);
                Ray dragRay = Camera.main.ScreenPointToRay(dragPos);
                if (groundPlane.Raycast(dragRay, out float distance))
                {
                    Vector3 pos = dragRay.GetPoint(distance);
                    pos.y += bottomOffset;
                    transform.position = pos;
                }
            }
        }
    }

    private void HandleScaleRotate()
    {
        bool currentlyScaling  = false;
        bool currentlyRotating = false;

        // Phone: 2-Finger Pinch and Twist
        if (Touch.activeTouches.Count == 2)
        {
            Touch t1 = Touch.activeTouches[0];
            Touch t2 = Touch.activeTouches[1];

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);
                if (initialPinchDistance < 10f) initialPinchDistance = 10f; 
                
                initialScale = transform.localScale;

                Vector2 dir = t2.screenPosition - t1.screenPosition;
                initialTwistAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                initialRotation = transform.rotation;

                // Reset snap-lock for this new gesture
                hasSnappedThisGesture = false;

                // Notify bounding box: gesture started
                if (boundingBox != null)
                {
                    boundingBox.OnScaleStart();
                    boundingBox.OnRotateStart();
                }
                wasScaling = true;
                wasRotating = true;
            }
            else if (t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved)
            {
                // SCALE — with snap-lock at optimal
                float currentPinchDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);
                if (initialPinchDistance > 0 && !hasSnappedThisGesture)
                {
                    float scaleFactor = currentPinchDistance / initialPinchDistance;
                    Vector3 targetScale = initialScale * scaleFactor;
                    targetScale = new Vector3(
                        Mathf.Clamp(targetScale.x, 0.1f, 5f),
                        Mathf.Clamp(targetScale.y, 0.1f, 5f),
                        Mathf.Clamp(targetScale.z, 0.1f, 5f)
                    );
                    transform.localScale = targetScale;
                    currentlyScaling = true;

                    // Check if we crossed into the optimal zone → snap and lock
                    if (boundingBox != null && boundingBox.IsAtOptimal())
                    {
                        // Snap to exact optimal scale
                        float optFactor = boundingBox.GetOptimalScaleFactor();
                        if (optFactor > 0)
                        {
                            Vector3 optScale = transform.localScale * optFactor;
                            optScale = new Vector3(
                                Mathf.Clamp(optScale.x, 0.1f, 5f),
                                Mathf.Clamp(optScale.y, 0.1f, 5f),
                                Mathf.Clamp(optScale.z, 0.1f, 5f)
                            );
                            transform.localScale = optScale;
                            snappedScale = optScale;
                        }
                        hasSnappedThisGesture = true;
                    }
                }
                else if (hasSnappedThisGesture)
                {
                    // Locked at optimal — don't allow further scale changes in this gesture
                    transform.localScale = snappedScale;
                    currentlyScaling = true;
                }

                // ROTATE (always allowed, even when scale is snapped)
                Vector2 dir = t2.screenPosition - t1.screenPosition;
                float currentTwistAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float deltaAngle = currentTwistAngle - initialTwistAngle;
                transform.rotation = initialRotation * Quaternion.Euler(0, -deltaAngle, 0);
                currentlyRotating = true;

                // Notify bounding box (only when values change)
                if (boundingBox != null)
                {
                    float pinchDelta = Mathf.Abs(currentPinchDistance - initialPinchDistance);
                    float twistDelta = Mathf.Abs(deltaAngle);

                    if (pinchDelta > twistDelta * 2f)
                    {
                        boundingBox.OnScaleUpdate();
                    }
                    else if (twistDelta > 5f)
                    {
                        boundingBox.OnRotateUpdate(transform.eulerAngles.y);
                    }
                    else
                    {
                        boundingBox.OnScaleUpdate();
                    }
                }
            }
        }
        // Editor: Mouse Scroll to Scale, Right-Click Drag to Rotate
        else if (Application.isEditor && UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue();
            if (scroll.y != 0)
            {
                if (!hasSnappedThisGesture)
                {
                    float scaleFactor = 1f + (Mathf.Sign(scroll.y) * 0.1f);
                    transform.localScale = new Vector3(
                        Mathf.Clamp(transform.localScale.x * scaleFactor, 0.1f, 5f),
                        Mathf.Clamp(transform.localScale.y * scaleFactor, 0.1f, 5f),
                        Mathf.Clamp(transform.localScale.z * scaleFactor, 0.1f, 5f)
                    );

                    // Check if we crossed into optimal
                    if (boundingBox != null && boundingBox.IsAtOptimal())
                    {
                        float optFactor = boundingBox.GetOptimalScaleFactor();
                        if (optFactor > 0)
                        {
                            Vector3 optScale = transform.localScale * optFactor;
                            transform.localScale = new Vector3(
                                Mathf.Clamp(optScale.x, 0.1f, 5f),
                                Mathf.Clamp(optScale.y, 0.1f, 5f),
                                Mathf.Clamp(optScale.z, 0.1f, 5f)
                            );
                            snappedScale = transform.localScale;
                        }
                        hasSnappedThisGesture = true;
                    }
                }
                currentlyScaling = true;

                if (boundingBox != null)
                {
                    if (!wasScaling) boundingBox.OnScaleStart();
                    boundingBox.OnScaleUpdate();
                }
                wasScaling = true;
            }
            else
            {
                // No scroll this frame → reset snap lock for next scroll
                if (wasScaling) hasSnappedThisGesture = false;
            }

            if (UnityEngine.InputSystem.Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                transform.Rotate(0, -delta.x * 0.5f, 0, Space.World);
                currentlyRotating = true;

                if (boundingBox != null)
                {
                    if (!wasRotating) boundingBox.OnRotateStart();
                    boundingBox.OnRotateUpdate(transform.eulerAngles.y);
                }
                wasRotating = true;
            }
        }

        // Detect gesture end
        if (wasScaling && !currentlyScaling)
        {
            if (boundingBox != null) boundingBox.OnScaleEnd();
            wasScaling = false;
            hasSnappedThisGesture = false; // Reset for next gesture
        }
        if (wasRotating && !currentlyRotating)
        {
            if (boundingBox != null) boundingBox.OnRotateEnd();
            wasRotating = false;
        }
    }

    public void Select()
    {
        if (SelectedObject != null && SelectedObject != this)
        {
            SelectedObject.Deselect();
        }

        SelectedObject = this;
        isSelected = true;

        // Show bounding box while selected
        if (boundingBox != null) boundingBox.Show();

        if (outlineMaterial != null)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    Material[] mats = new Material[originalMaterials[r].Length + 1];
                    for (int i = 0; i < originalMaterials[r].Length; i++)
                    {
                        mats[i] = originalMaterials[r][i];
                    }
                    mats[mats.Length - 1] = outlineMaterial;
                    r.materials = mats;
                }
            }
        }
    }

    public void Deselect()
    {
        isSelected = false;
        isInMoveMode = false;
        if (SelectedObject == this) SelectedObject = null;

        // Hide bounding box on deselect
        if (boundingBox != null) boundingBox.Hide();

        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.materials = originalMaterials[r];
            }
        }
    }

    void OnDestroy()
    {
        if (SelectedObject == this) SelectedObject = null;
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
            Debug.Log($"[ARStandaloneManipulator] IsTouchOverUI: hit UI element '{result.gameObject.name}' on layer {result.gameObject.layer}");
            // Only block if we hit an actual interactive UI element, not background panels
            if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null ||
                result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                result.gameObject.GetComponentInParent<UnityEngine.UI.Toggle>() != null)
            {
                Debug.Log($"[ARStandaloneManipulator] Blocking click/touch because it hit interactive UI element '{result.gameObject.name}'");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Prevents the object from passing through detected vertical (wall) planes.
    /// Calculates the object's half-extents and pushes it back if its bounding box
    /// would cross any wall surface.
    /// </summary>
    private Vector3 ClampPositionToWalls(Vector3 targetPos)
    {
        if (cachedPlaneManager == null) cachedPlaneManager = FindObjectOfType<ARPlaneManager>();
        if (cachedPlaneManager == null) return targetPos;
        ARPlaneManager planeManager = cachedPlaneManager;

        // Get object half-extents in world space
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null) return targetPos;

        // Calculate the world-space half-size of the collider
        Vector3 halfExtents = Vector3.Scale(bc.size, transform.localScale) * 0.5f;
        float objectRadius = Mathf.Max(halfExtents.x, halfExtents.z); // Use the larger horizontal extent

        foreach (var plane in planeManager.trackables)
        {
            // Only check vertical planes (walls)
            if (plane.alignment != PlaneAlignment.Vertical) continue;
            if (!plane.gameObject.activeInHierarchy) continue;

            // Wall plane definition: a point on the wall and its outward-facing normal
            Vector3 wallPos = plane.transform.position;
            Vector3 wallNormal = plane.transform.up; // AR planes use 'up' as the surface normal

            // Signed distance from the object center to the wall plane
            // Positive = in front of wall (safe), Negative = behind/inside wall (bad)
            float signedDist = Vector3.Dot(targetPos - wallPos, wallNormal);

            // If object center is too close to or behind the wall, push it out
            float minDistance = objectRadius + 0.02f; // 2cm buffer
            if (signedDist < minDistance && signedDist > -0.5f) // Only clamp if reasonably close, not on the other side of the room
            {
                targetPos += wallNormal * (minDistance - signedDist);
            }
        }

        return targetPos;
    }

    public void ScaleToOptimal()
    {
        if (boundingBox != null)
        {
            float optFactor = boundingBox.GetOptimalScaleFactor();
            if (optFactor > 0)
            {
                transform.localScale = transform.localScale * optFactor;
                // Clamp scale to safety bounds
                transform.localScale = new Vector3(
                    Mathf.Clamp(transform.localScale.x, 0.1f, 5f),
                    Mathf.Clamp(transform.localScale.y, 0.1f, 5f),
                    Mathf.Clamp(transform.localScale.z, 0.1f, 5f)
                );
                
                // Refresh bounding box visualizer lines/labels
                boundingBox.OnScaleUpdate();
                boundingBox.OnScaleEnd();
                
                if (debugText != null) debugText.text = "Scaled to optimal size!";
                Debug.Log($"ScaleToOptimal: Object scaled to optimal scale factor. New scale: {transform.localScale}");
            }
            else
            {
                if (debugText != null) debugText.text = "No optimal size set for this product.";
            }
        }
    }

    public void DeleteObject()
    {
        Debug.Log($"DeleteObject: Soft-deleting (pooling) object '{gameObject.name}'");
        
        // Remove selection reference
        if (SelectedObject == this)
        {
            Deselect();
        }
        
        // Pool the object instead of hard destroying it
        if (PlacedObjectsManager.Instance != null)
        {
            PlacedObjectsManager.Instance.PoolPlacedObject(gameObject);
        }
        else
        {
            // Fallback: hard destroy if manager is missing
            Destroy(gameObject);
        }
        
        if (debugText != null) debugText.text = "Product deleted.";
    }
}
