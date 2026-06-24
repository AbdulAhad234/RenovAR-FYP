using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARRaycastPlacement : MonoBehaviour
{
    public GameObject placementIndicator;
    public GameObject objectToPlace;

    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private Pose placementPose;
    private bool placementValid = false;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        anchorManager = GetComponent<ARAnchorManager>();
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
        HandlePlacement();
    }

    void UpdatePlacementPose()
    {
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            placementValid = false;
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, hits,
            TrackableType.PlaneWithinPolygon |
            TrackableType.PlaneEstimated |
            TrackableType.FeaturePoint))
        {
            placementPose = hits[0].pose;
            placementValid = true;
        }
        else
        {
            placementValid = false;
        }
    }

    void UpdatePlacementIndicator()
    {
        if (placementValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(
                placementPose.position,
                placementPose.rotation
            );
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    void HandlePlacement()
    {
        if (!placementValid)
            return;

        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            GameObject spawned = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
            spawned.AddComponent<ARAnchor>();

 
        }
    }
}
