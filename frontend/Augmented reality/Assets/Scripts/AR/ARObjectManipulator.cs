using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARObjectManipulator : MonoBehaviour
{
    public static ARObjectManipulator SelectedObject { get; private set; }

    [HideInInspector] public Material outlineMaterial;
    
    private List<Renderer> renderers = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    private ARRaycastManager raycastManager;
    private bool isSelected = false;

    // Gesture tracking
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float initialTwistAngle;
    private Quaternion initialRotation;
    
    // Bounds check to avoid raycasting against our own UI
    private Collider objectCollider;

    void Awake()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends)
        {
            renderers.Add(r);
            originalMaterials[r] = r.materials;
        }
        
        objectCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (Touch.activeTouches.Count == 0) return;

        // Selection Logic on First Tap
        if (Touch.activeTouches[0].phase == TouchPhase.Began)
        {
            Vector2 touchPos = Touch.activeTouches[0].screenPosition;
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            
            RaycastHit[] hits = Physics.RaycastAll(ray);
            bool hitOurObject = false;

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                {
                    hitOurObject = true;
                    break;
                }
            }

            if (hitOurObject)
            {
                Select();
                return; // Don't do translation on the very first tap frame
            }
            else if (SelectedObject == this)
            {
                Deselect();
            }
        }

        if (!isSelected) return;

        // 1-Finger Drag (Translation)
        if (Touch.activeTouches.Count == 1)
        {
            Touch touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Moved)
            {
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                // Only move if we are dragging on the floor plane
                if (raycastManager != null && raycastManager.Raycast(touch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    transform.position = hits[0].pose.position;
                }
            }
        }
        // 2-Finger Pinch and Twist (Scale and Rotate)
        else if (Touch.activeTouches.Count == 2)
        {
            Touch t1 = Touch.activeTouches[0];
            Touch t2 = Touch.activeTouches[1];

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);
                initialScale = transform.localScale;

                Vector2 dir = t2.screenPosition - t1.screenPosition;
                initialTwistAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                initialRotation = transform.rotation;
            }
            else if (t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved)
            {
                // -- SCALE --
                float currentPinchDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);
                if (initialPinchDistance > 0)
                {
                    float scaleFactor = currentPinchDistance / initialPinchDistance;
                    Vector3 targetScale = initialScale * scaleFactor;
                    
                    // Clamp scale to prevent it getting too big or small
                    targetScale = new Vector3(
                        Mathf.Clamp(targetScale.x, 0.1f, 5f),
                        Mathf.Clamp(targetScale.y, 0.1f, 5f),
                        Mathf.Clamp(targetScale.z, 0.1f, 5f)
                    );
                    
                    transform.localScale = targetScale;
                }

                // -- ROTATE --
                Vector2 dir = t2.screenPosition - t1.screenPosition;
                float currentTwistAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float deltaAngle = currentTwistAngle - initialTwistAngle;
                
                // Unity UI touches map inversely to Unity 3D Y axis rotations depending on phone orientation, 
                // but -deltaAngle is standard for twist.
                transform.rotation = initialRotation * Quaternion.Euler(0, -deltaAngle, 0);
            }
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
        if (SelectedObject == this) SelectedObject = null;

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
}
