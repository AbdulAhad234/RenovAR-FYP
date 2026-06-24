using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Draws a 3D wireframe bounding box around an AR-placed object.
/// Shows real-world W x H dimensions during scaling, with an "optimal size" snap.
/// Shows rotation degrees during rotation.
/// Automatically created by ARStandaloneManipulator when an object is selected.
/// 
/// PERFORMANCE: Renderers are cached at creation. Text/color updates use dirty-flag
/// comparisons to avoid unnecessary TMP rebuilds and material property changes.
/// </summary>
public class BoundingBoxVisualizer : MonoBehaviour
{
    // ── Configuration ──────────────────────────────────────────────
    [Header("Visual Settings")]
    public Color defaultLineColor  = new Color(1f, 1f, 1f, 0.85f);      // White
    public Color optimalColor      = new Color(0.2f, 1f, 0.4f, 1f);     // Bright green
    public Color tooSmallColor     = new Color(1f, 0.6f, 0.2f, 0.85f);  // Orange
    public Color tooBigColor       = new Color(1f, 0.3f, 0.3f, 0.85f);  // Red-ish
    public float lineWidth         = 0.003f; // 3mm — thin but visible in AR
    public TMP_FontAsset fontAsset; // Custom Poppins-Light SDF font asset

    [Header("Optimal Size (set by ARStandaloneManipulator)")]
    public float optimalWidth  = -1f;  // metres
    public float optimalHeight = -1f;
    public float optimalDepth  = -1f;
    public float snapThreshold = 0.04f; // 4 cm tolerance for "optimal" zone

    // ── Runtime state ──────────────────────────────────────────────
    private LineRenderer[] edgeLines = new LineRenderer[12];
    private TextMeshPro widthLabel;
    private GameObject widthLabelGO;
    private TextMeshPro heightLabel;
    private GameObject heightLabelGO;

    private GameObject optimalButtonGO;
    private GameObject deleteButtonGO;

    public GameObject OptimalButton => optimalButtonGO;
    public GameObject DeleteButton => deleteButtonGO;

    private bool isScaling  = false;
    private bool isRotating = false;
    private bool isVisible  = false;

    private Coroutine greenFlashRoutine;

    // ── Performance caches ─────────────────────────────────────────
    private Renderer[] cachedRenderers;     // Cached once at creation, excludes our own lines/labels
    private string lastWidthLabelText = "";  // Avoid TMP rebuild if width text unchanged
    private string lastHeightLabelText = ""; // Avoid TMP rebuild if height text unchanged
    private Color lastLabelColor;           // Avoid material change if color unchanged
    private Color lastEdgeColor;            // Avoid 12x material change if color unchanged
    private Bounds lastBounds;              // Avoid recalculating edges when bounds haven't changed
    private bool boundsInitialized = false;
    private Material sharedLineMaterial;    // One shared material instead of 12 separate ones

    public float extraYOffset = 0.08f;
    public float extraXOffset = 0.06f;

    // ── Public API (called by ARStandaloneManipulator) ─────────────

    public void Show()
    {
        if (isVisible) return;
        isVisible = true;
        CreateVisualsIfNeeded();
        SetAllActive(true);
        RefreshBoundsAndEdges();
        UpdateDimensionLabel();
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        SetAllActive(false);
    }

    public void OnScaleStart()
    {
        isScaling = true;
        isRotating = false;
        if (heightLabelGO != null && !heightLabelGO.activeSelf && isVisible)
        {
            heightLabelGO.SetActive(true);
        }
    }

    public void OnScaleUpdate()
    {
        if (!isVisible) return;
        RefreshBoundsAndEdges();
        UpdateDimensionLabel();
        UpdateEdgeColorsForState();
    }

    public void OnScaleEnd()
    {
        isScaling = false;
        SetLabelColors(defaultLineColor);
        SetEdgeColors(defaultLineColor);
    }

    public void OnRotateStart()
    {
        isRotating = true;
        isScaling = false;
        if (heightLabelGO != null && heightLabelGO.activeSelf)
        {
            heightLabelGO.SetActive(false);
        }
    }

    public void OnRotateUpdate(float currentYRotation)
    {
        if (!isVisible) return;
        RefreshBoundsAndEdges();
        UpdateRotationLabel(currentYRotation);
    }

    public void OnRotateEnd()
    {
        isRotating = false;
        if (heightLabelGO != null && !heightLabelGO.activeSelf && isVisible)
        {
            heightLabelGO.SetActive(true);
        }
        UpdateDimensionLabel();
    }

    /// <summary>
    /// Returns the uniform scale factor needed to reach optimal height.
    /// Returns -1 if no optimal height is set or renderers are missing.
    /// </summary>
    public float GetOptimalScaleFactor()
    {
        if (optimalHeight <= 0) return -1f;
        float currentH = GetLocalBounds().size.y * transform.lossyScale.y;
        if (currentH <= 0.001f) return -1f;
        return optimalHeight / currentH;
    }

    /// <summary>
    /// Returns the current SizeState.
    /// </summary>
    public bool IsAtOptimal()
    {
        if (optimalHeight <= 0 && optimalWidth <= 0) return false;
        return GetSizeState() == SizeState.Optimal;
    }

    // ── Create Visuals ─────────────────────────────────────────────

    private void CreateVisualsIfNeeded()
    {
        if (edgeLines[0] != null) return;

        // Cache renderers for bounds calculation (exclude our own objects)
        CacheRenderers();

        // Create a shared material for all 12 lines (huge perf win vs 12 separate materials)
        sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
        sharedLineMaterial.color = defaultLineColor;

        GameObject container = new GameObject("__BoundingBox__");
        container.transform.SetParent(transform, false);

        for (int i = 0; i < 12; i++)
        {
            GameObject lineGO = new GameObject($"Edge_{i}");
            lineGO.transform.SetParent(container.transform, false);

            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.numCornerVertices = 0; // Reduced from 2 — cheaper to render
            lr.numCapVertices = 0;    // Reduced from 2
            lr.material = sharedLineMaterial; // Shared, not new per line
            lr.startColor = defaultLineColor;
            lr.endColor = defaultLineColor;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            edgeLines[i] = lr;
        }

        // Width label (3D TextMeshPro) - on top
        widthLabelGO = new GameObject("__WidthLabel__");
        widthLabelGO.transform.SetParent(container.transform, false);

        widthLabel = widthLabelGO.AddComponent<TextMeshPro>();
        if (fontAsset != null) widthLabel.font = fontAsset;
        widthLabel.fontSize = 40f;
        widthLabel.alignment = TextAlignmentOptions.Center;
        widthLabel.color = defaultLineColor;
        widthLabel.fontStyle = FontStyles.Bold;
        widthLabel.enableWordWrapping = false;
        widthLabel.overflowMode = TextOverflowModes.Overflow;
        widthLabel.sortingOrder = 100;

        RectTransform rtWidth = widthLabelGO.GetComponent<RectTransform>();
        rtWidth.sizeDelta = new Vector2(4f, 1f);

        if (widthLabelGO.GetComponent<Billboard>() == null)
        {
            widthLabelGO.AddComponent<Billboard>();
        }

        // Height label (3D TextMeshPro) - on the right
        heightLabelGO = new GameObject("__HeightLabel__");
        heightLabelGO.transform.SetParent(container.transform, false);

        heightLabel = heightLabelGO.AddComponent<TextMeshPro>();
        if (fontAsset != null) heightLabel.font = fontAsset;
        heightLabel.fontSize = 25f;
        heightLabel.alignment = TextAlignmentOptions.Center;
        heightLabel.color = defaultLineColor;
        heightLabel.fontStyle = FontStyles.Bold;
        heightLabel.enableWordWrapping = false;
        heightLabel.overflowMode = TextOverflowModes.Overflow;
        heightLabel.sortingOrder = 100;

        RectTransform rtHeight = heightLabelGO.GetComponent<RectTransform>();
        rtHeight.sizeDelta = new Vector2(4f, 1f);

        if (heightLabelGO.GetComponent<Billboard>() == null)
        {
            heightLabelGO.AddComponent<Billboard>();
        }

        lastEdgeColor = defaultLineColor;
        lastLabelColor = defaultLineColor;

        CreateBypassButtons();
    }

    private Sprite LoadSpriteFromResources(string resourceName)
    {
        // Try to load directly as Sprite (if imported as Sprite (2D and UI))
        Sprite spr = Resources.Load<Sprite>(resourceName);
        if (spr != null) return spr;

        // Fallback: load as Texture2D and create Sprite (if imported as default Texture)
        Texture2D tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null)
        {
            Debug.Log($"BoundingBoxVisualizer: Loaded '{resourceName}' as Texture2D and wrapping it as a Sprite at runtime.");
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        Debug.LogError($"BoundingBoxVisualizer: Failed to load '{resourceName}' from Resources as both Sprite and Texture2D!");
        return null;
    }

    private void CreateBypassButtons()
    {
        ARStandaloneManipulator manipulator = GetComponent<ARStandaloneManipulator>();

        // Optimal Button (Sprite Renderer + Box Collider)
        optimalButtonGO = new GameObject("__OptimalButton__");
        optimalButtonGO.transform.SetParent(null);
        optimalButtonGO.transform.position = Vector3.zero;
        optimalButtonGO.transform.rotation = Quaternion.identity;
        optimalButtonGO.transform.localScale = Vector3.one;

        SpriteRenderer srOpt = optimalButtonGO.AddComponent<SpriteRenderer>();
        Sprite optSprite = LoadSpriteFromResources("Optimal");
        if (optSprite != null)
        {
            srOpt.sprite = optSprite;
        }
        srOpt.sortingOrder = 101;

        BoxCollider bcOpt = optimalButtonGO.AddComponent<BoxCollider>();
        if (srOpt.sprite != null)
        {
            bcOpt.size = new Vector3(srOpt.sprite.bounds.size.x * 1.2f, srOpt.sprite.bounds.size.y * 1.2f, 0.5f);
        }
        else
        {
            bcOpt.size = new Vector3(1.2f, 1.2f, 0.5f);
        }

        // Attach action click handler script
        ARActionButton actOpt = optimalButtonGO.AddComponent<ARActionButton>();
        actOpt.onClick = () =>
        {
            if (manipulator != null) manipulator.ScaleToOptimal();
        };

        // Delete Button (Sprite Renderer + Box Collider)
        deleteButtonGO = new GameObject("__DeleteButton__");
        deleteButtonGO.transform.SetParent(null);
        deleteButtonGO.transform.position = Vector3.zero;
        deleteButtonGO.transform.rotation = Quaternion.identity;
        deleteButtonGO.transform.localScale = Vector3.one;

        SpriteRenderer srDel = deleteButtonGO.AddComponent<SpriteRenderer>();
        Sprite delSprite = LoadSpriteFromResources("deleteSelected");
        if (delSprite != null)
        {
            srDel.sprite = delSprite;
        }
        srDel.sortingOrder = 101;

        BoxCollider bcDel = deleteButtonGO.AddComponent<BoxCollider>();
        if (srDel.sprite != null)
        {
            bcDel.size = new Vector3(srDel.sprite.bounds.size.x * 1.2f, srDel.sprite.bounds.size.y * 1.2f, 0.5f);
        }
        else
        {
            bcDel.size = new Vector3(1.2f, 1.2f, 0.5f);
        }

        // Attach action click handler script
        ARActionButton actDel = deleteButtonGO.AddComponent<ARActionButton>();
        actDel.onClick = () =>
        {
            if (manipulator != null) manipulator.DeleteObject();
        };
    }

    /// <summary>
    /// Caches the list of renderers used for bounds calculation, filtering out
    /// our own LineRenderers and label objects. Called once during creation.
    /// </summary>
    private void CacheRenderers()
    {
        Renderer[] allRends = GetComponentsInChildren<Renderer>();
        var filtered = new List<Renderer>();
        foreach (Renderer r in allRends)
        {
            if (r is LineRenderer) continue;
            if (r.gameObject.name.StartsWith("__")) continue;
            filtered.Add(r);
        }
        cachedRenderers = filtered.ToArray();
    }

    // ── Update edge positions ──────────────────────────────────────

    private void RefreshBoundsAndEdges()
    {
        Bounds localB = GetLocalBounds();
        Vector3 min = localB.min;
        Vector3 max = localB.max;

        // 8 corners in local space
        Vector3 c0 = new Vector3(min.x, min.y, min.z);
        Vector3 c1 = new Vector3(max.x, min.y, min.z);
        Vector3 c2 = new Vector3(max.x, min.y, max.z);
        Vector3 c3 = new Vector3(min.x, min.y, max.z);
        Vector3 c4 = new Vector3(min.x, max.y, min.z);
        Vector3 c5 = new Vector3(max.x, max.y, min.z);
        Vector3 c6 = new Vector3(max.x, max.y, max.z);
        Vector3 c7 = new Vector3(min.x, max.y, max.z);

        // Transform corners to world space
        Vector3 w0 = transform.TransformPoint(c0);
        Vector3 w1 = transform.TransformPoint(c1);
        Vector3 w2 = transform.TransformPoint(c2);
        Vector3 w3 = transform.TransformPoint(c3);
        Vector3 w4 = transform.TransformPoint(c4);
        Vector3 w5 = transform.TransformPoint(c5);
        Vector3 w6 = transform.TransformPoint(c6);
        Vector3 w7 = transform.TransformPoint(c7);

        // Bottom face
        SetEdge(0, w0, w1);
        SetEdge(1, w1, w2);
        SetEdge(2, w2, w3);
        SetEdge(3, w3, w0);
        // Top face
        SetEdge(4, w4, w5);
        SetEdge(5, w5, w6);
        SetEdge(6, w6, w7);
        SetEdge(7, w7, w4);
        // Vertical pillars
        SetEdge(8,  w0, w4);
        SetEdge(9,  w1, w5);
        SetEdge(10, w2, w6);
        SetEdge(11, w3, w7);

        // Compute world AABB from rotated corners for label positioning
        Bounds worldBounds = new Bounds(w0, Vector3.zero);
        worldBounds.Encapsulate(w1);
        worldBounds.Encapsulate(w2);
        worldBounds.Encapsulate(w3);
        worldBounds.Encapsulate(w4);
        worldBounds.Encapsulate(w5);
        worldBounds.Encapsulate(w6);
        worldBounds.Encapsulate(w7);

        // Skip update if bounds haven't meaningfully changed (< 1mm difference)
        if (boundsInitialized
            && Mathf.Abs(worldBounds.center.x - lastBounds.center.x) < 0.001f
            && Mathf.Abs(worldBounds.center.y - lastBounds.center.y) < 0.001f
            && Mathf.Abs(worldBounds.center.z - lastBounds.center.z) < 0.001f
            && Mathf.Abs(worldBounds.size.x - lastBounds.size.x) < 0.001f
            && Mathf.Abs(worldBounds.size.y - lastBounds.size.y) < 0.001f
            && Mathf.Abs(worldBounds.size.z - lastBounds.size.z) < 0.001f)
        {
            return;
        }

        lastBounds = worldBounds;
        boundsInitialized = true;

        // Position labels relative to bounds and camera perspective
        UpdateLabelPositions();
    }

    private void UpdateLabelPositions()
    {
        if (!boundsInitialized) return;

        Bounds bounds = lastBounds;
        Vector3 parentScale = transform.lossyScale;

        // Position width label above bounding box
        if (widthLabelGO != null)
        {
            float labelOffset = Mathf.Max(bounds.size.y * 0.08f, 0.05f);
            widthLabelGO.transform.position =
                bounds.center + Vector3.up * (bounds.extents.y + labelOffset + extraYOffset);

            float labelScale = Mathf.Clamp(bounds.size.magnitude * 0.02f, 0.005f, 0.025f);
            widthLabelGO.transform.localScale = new Vector3(
                labelScale / Mathf.Max(parentScale.x, 0.001f),
                labelScale / Mathf.Max(parentScale.y, 0.001f),
                labelScale / Mathf.Max(parentScale.z, 0.001f)
            );

            // Position and scale the action buttons in world space to keep them large and clickable
            if (optimalButtonGO != null && deleteButtonGO != null)
            {
                Camera cam = Camera.main;
                Vector3 rightDir = cam != null ? cam.transform.right : Vector3.right;
                Vector3 upDir = cam != null ? cam.transform.up : Vector3.up;

                float buttonSize = 0.11f; // 11 cm in world space (larger, more readable, easier to tap)
                float spacing = 0.09f;    // 9 cm spacing left and right (gap of 7cm between button edges)
                Vector3 center = widthLabelGO.transform.position + upDir * 0.12f; // 12 cm above width text

                optimalButtonGO.transform.position = center - rightDir * spacing;
                deleteButtonGO.transform.position = center + rightDir * spacing;

                // Match widthLabelGO's billboard rotation so buttons face the camera perfectly
                optimalButtonGO.transform.rotation = widthLabelGO.transform.rotation;
                deleteButtonGO.transform.rotation = widthLabelGO.transform.rotation;

                float optSpriteWidth = 1f;
                float optSpriteHeight = 1f;
                SpriteRenderer srOpt = optimalButtonGO.GetComponent<SpriteRenderer>();
                if (srOpt != null && srOpt.sprite != null)
                {
                    optSpriteWidth = srOpt.sprite.bounds.size.x;
                    optSpriteHeight = srOpt.sprite.bounds.size.y;
                }

                float delSpriteWidth = 1f;
                float delSpriteHeight = 1f;
                SpriteRenderer srDel = deleteButtonGO.GetComponent<SpriteRenderer>();
                if (srDel != null && srDel.sprite != null)
                {
                    delSpriteWidth = srDel.sprite.bounds.size.x;
                    delSpriteHeight = srDel.sprite.bounds.size.y;
                }

                // If bounds are 0, default to 1.0 to avoid division by zero
                if (optSpriteWidth <= 0f) optSpriteWidth = 1f;
                if (optSpriteHeight <= 0f) optSpriteHeight = 1f;
                if (delSpriteWidth <= 0f) delSpriteWidth = 1f;
                if (delSpriteHeight <= 0f) delSpriteHeight = 1f;

                // Scale directly in world space (no parent scale division needed!)
                optimalButtonGO.transform.localScale = new Vector3(
                    buttonSize / optSpriteWidth,
                    buttonSize / optSpriteHeight,
                    buttonSize
                );

                deleteButtonGO.transform.localScale = new Vector3(
                    buttonSize / delSpriteWidth,
                    buttonSize / delSpriteHeight,
                    buttonSize
                );
            }
        }

        // Position height label on the right side of the bounding box relative to the camera
        if (heightLabelGO != null && heightLabelGO.activeSelf)
        {
            Camera cam = Camera.main;
            Vector3 rightDir = cam != null ? cam.transform.right : Vector3.right;
            
            // Project the bounds horizontal extents onto the camera right direction
            float extentRight = bounds.extents.x * Mathf.Abs(rightDir.x) + bounds.extents.z * Mathf.Abs(rightDir.z);
            
            // Position it cleanly outside the right edge of the bounding box
            float labelOffset = Mathf.Max(extentRight * 0.08f, 0.04f) + extraXOffset;
            float horizontalOffset = extentRight + labelOffset;

            heightLabelGO.transform.position =
                bounds.center + rightDir * horizontalOffset;

            float labelScale = Mathf.Clamp(bounds.size.magnitude * 0.02f, 0.005f, 0.025f);
            heightLabelGO.transform.localScale = new Vector3(
                labelScale / Mathf.Max(parentScale.x, 0.001f),
                labelScale / Mathf.Max(parentScale.y, 0.001f),
                labelScale / Mathf.Max(parentScale.z, 0.001f)
            );
        }
    }

    void LateUpdate()
    {
        if (!isVisible) return;
        UpdateLabelPositions();
    }

    private void SetEdge(int index, Vector3 from, Vector3 to)
    {
        if (edgeLines[index] == null) return;
        edgeLines[index].SetPosition(0, from);
        edgeLines[index].SetPosition(1, to);
    }

    // ── Labels (with dirty-flag optimization) ──────────────────────

    private void UpdateDimensionLabel()
    {
        if (!boundsInitialized) return;

        Bounds localB = GetLocalBounds();
        Vector3 parentScale = transform.lossyScale;
        float wCm = localB.size.x * parentScale.x * 100f;
        float hCm = localB.size.y * parentScale.y * 100f;

        // Build text — only update TMP if text actually changed
        string newWidthText = $"{wCm:F0} cm W";
        if (widthLabel != null && newWidthText != lastWidthLabelText)
        {
            widthLabel.text = newWidthText;
            lastWidthLabelText = newWidthText;
        }

        string newHeightText = $"{hCm:F0} cm H";
        if (heightLabel != null && newHeightText != lastHeightLabelText)
        {
            heightLabel.text = newHeightText;
            lastHeightLabelText = newHeightText;
        }

        // Color based on state — only update if color actually changed
        if (isScaling)
        {
            SizeState state = GetSizeState();
            Color c;
            switch (state)
            {
                case SizeState.TooSmall: c = tooSmallColor; break;
                case SizeState.TooBig:   c = tooBigColor;   break;
                case SizeState.Optimal:  c = optimalColor;  break;
                default:                 c = defaultLineColor; break;
            }
            SetLabelColors(c);
        }
    }

    private void UpdateRotationLabel(float yRotation)
    {
        if (widthLabel == null) return;

        float deg = yRotation % 360f;
        if (deg < 0f) deg += 360f;

        string newText = $"{deg:F1}°";
        if (newText != lastWidthLabelText)
        {
            widthLabel.text = newText;
            lastWidthLabelText = newText;
        }

        SetLabelColors(defaultLineColor);

        // Hide height label during rotation
        if (heightLabelGO != null && heightLabelGO.activeSelf)
        {
            heightLabelGO.SetActive(false);
        }
    }

    private void SetLabelColors(Color c)
    {
        if (c != lastLabelColor)
        {
            if (widthLabel != null) widthLabel.color = c;
            if (heightLabel != null) heightLabel.color = c;
            lastLabelColor = c;
        }
    }

    // ── Optimal snap ───────────────────────────────────────────────

    public enum SizeState { Normal, TooSmall, TooBig, Optimal }

    public SizeState GetSizeState()
    {
        if (optimalWidth <= 0 && optimalHeight <= 0) return SizeState.Normal;

        Bounds localB = GetLocalBounds();
        Vector3 parentScale = transform.lossyScale;
        float currentW = localB.size.x * parentScale.x;
        float currentH = localB.size.y * parentScale.y;

        bool widthOk  = optimalWidth  <= 0 || Mathf.Abs(currentW - optimalWidth)  < snapThreshold;
        bool heightOk = optimalHeight <= 0 || Mathf.Abs(currentH - optimalHeight) < snapThreshold;

        if (widthOk && heightOk) return SizeState.Optimal;

        if (optimalHeight > 0)
        {
            if (currentH < optimalHeight - snapThreshold) return SizeState.TooSmall;
            if (currentH > optimalHeight + snapThreshold) return SizeState.TooBig;
        }

        return SizeState.Normal;
    }

    private void UpdateEdgeColorsForState()
    {
        if (optimalWidth <= 0 && optimalHeight <= 0) return;
        if (!boundsInitialized) return;

        SizeState state = GetSizeState();

        if (state == SizeState.Optimal)
        {
            if (greenFlashRoutine != null) StopCoroutine(greenFlashRoutine);
            greenFlashRoutine = StartCoroutine(GreenFlash());
        }
        else
        {
            Color c;
            switch (state)
            {
                case SizeState.TooSmall: c = tooSmallColor; break;
                case SizeState.TooBig:   c = tooBigColor;   break;
                default:                 c = defaultLineColor; break;
            }
            SetEdgeColors(c);
        }
    }

    private IEnumerator GreenFlash()
    {
        SetEdgeColors(optimalColor);
        SetLabelColors(optimalColor);

        yield return new WaitForSeconds(1.0f);

        SetEdgeColors(defaultLineColor);
        SetLabelColors(defaultLineColor);
        greenFlashRoutine = null;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private Bounds CalculateWorldBounds()
    {
        // Use cached renderers (no per-frame GetComponentsInChildren)
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one * 0.1f);
        }

        Bounds bounds = default;
        bool first = true;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r == null) continue;

            if (first)
            {
                bounds = r.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return first ? new Bounds(transform.position, Vector3.one * 0.1f) : bounds;
    }

    private Bounds cachedLocalBounds;
    private bool localBoundsCached = false;

    private Bounds GetLocalBounds()
    {
        if (!localBoundsCached)
        {
            cachedLocalBounds = CalculateLocalBounds();
            localBoundsCached = true;
        }
        return cachedLocalBounds;
    }

    private Bounds CalculateLocalBounds()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.1f);
        }

        Bounds localBounds = default;
        bool first = true;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r == null) continue;

            Bounds rendererLocalBounds;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                rendererLocalBounds = mf.sharedMesh.bounds;
            }
            else
            {
                SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
                if (smr != null && smr.sharedMesh != null)
                {
                    rendererLocalBounds = smr.sharedMesh.bounds;
                }
                else
                {
                    Vector3 worldCenter = r.bounds.center;
                    Vector3 worldExtents = r.bounds.extents;
                    Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
                    Vector3 localExtents = transform.InverseTransformVector(worldExtents);
                    localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));
                    rendererLocalBounds = new Bounds(localCenter, localExtents * 2f);

                    if (first)
                    {
                        localBounds = rendererLocalBounds;
                        first = false;
                    }
                    else
                    {
                        localBounds.Encapsulate(rendererLocalBounds);
                    }
                    continue;
                }
            }

            Matrix4x4 localToRoot = transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
            Vector3 ext = rendererLocalBounds.extents;
            Vector3 cnt = rendererLocalBounds.center;

            Vector3[] corners = new Vector3[]
            {
                cnt + new Vector3(-ext.x, -ext.y, -ext.z),
                cnt + new Vector3( ext.x, -ext.y, -ext.z),
                cnt + new Vector3( ext.x, -ext.y,  ext.z),
                cnt + new Vector3(-ext.x, -ext.y,  ext.z),
                cnt + new Vector3(-ext.x,  ext.y, -ext.z),
                cnt + new Vector3( ext.x,  ext.y, -ext.z),
                cnt + new Vector3( ext.x,  ext.y,  ext.z),
                cnt + new Vector3(-ext.x,  ext.y,  ext.z)
            };

            for (int j = 0; j < 8; j++)
            {
                Vector3 lp = localToRoot.MultiplyPoint3x4(corners[j]);
                if (first)
                {
                    localBounds = new Bounds(lp, Vector3.zero);
                    first = false;
                }
                else
                {
                    localBounds.Encapsulate(lp);
                }
            }
        }

        return first ? new Bounds(Vector3.zero, Vector3.one * 0.1f) : localBounds;
    }

    private void SetEdgeColors(Color c)
    {
        // Skip if color hasn't changed
        if (c == lastEdgeColor) return;
        lastEdgeColor = c;

        for (int i = 0; i < 12; i++)
        {
            if (edgeLines[i] != null)
            {
                edgeLines[i].startColor = c;
                edgeLines[i].endColor = c;
            }
        }
    }

    private void SetAllActive(bool active)
    {
        for (int i = 0; i < 12; i++)
        {
            if (edgeLines[i] != null) edgeLines[i].gameObject.SetActive(active);
        }
        if (widthLabelGO != null) widthLabelGO.SetActive(active);
        if (heightLabelGO != null) heightLabelGO.SetActive(active && !isRotating);
        if (optimalButtonGO != null) optimalButtonGO.SetActive(active);
        if (deleteButtonGO != null) deleteButtonGO.SetActive(active);
    }

    void OnDestroy()
    {
        if (greenFlashRoutine != null) StopCoroutine(greenFlashRoutine);
        if (sharedLineMaterial != null) Destroy(sharedLineMaterial);
        if (optimalButtonGO != null) Destroy(optimalButtonGO);
        if (deleteButtonGO != null) Destroy(deleteButtonGO);
    }
}

public class ARActionButton : MonoBehaviour
{
    public System.Action onClick;

    void OnMouseDown()
    {
        if (enabled && gameObject.activeInHierarchy)
        {
            onClick?.Invoke();
        }
    }
}
