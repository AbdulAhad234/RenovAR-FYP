using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Collections.Generic;

public class PlaneClassificationVisualizer : MonoBehaviour
{
    public GameObject labelPrefab;
    public bool showLabels = true; // Toggle in inspector
    private ARPlaneManager planeManager;

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (!showLabels) return;

        foreach (var plane in args.added)
        {
            CreateOrUpdateLabel(plane);
        }

        foreach (var plane in args.updated)
        {
            CreateOrUpdateLabel(plane);
        }

        foreach (var plane in args.removed)
        {
            Transform existing = plane.transform.Find("PlaneLabel");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }
    }

    void CreateOrUpdateLabel(ARPlane plane)
    {
        // Don't label tiny planes
        if (plane.extents.x * plane.extents.y < 0.1f)
            return;

        Transform existing = plane.transform.Find("PlaneLabel");
        GameObject label;

        if (existing == null)
        {
            label = Instantiate(labelPrefab, plane.transform);
            label.name = "PlaneLabel";
            label.transform.localScale = Vector3.one * 0.01f; // Slightly bigger
        }
        else
        {
            label = existing.gameObject;
        }

        // Position at center of plane
        label.transform.localPosition = new Vector3(0, 0.01f, 0);
        label.transform.localRotation = Quaternion.identity;

        var text = label.GetComponent<TextMeshPro>();
        text.text = GetPlaneTypeText(plane);
        text.alignment = TextAlignmentOptions.Center;
    }

    string GetPlaneTypeText(ARPlane plane)
    {
        var classifications = plane.classifications;
        float area = plane.extents.x * plane.extents.y;

        // Try semantic classification first
        if (classifications != PlaneClassifications.None)
        {
            List<string> types = new List<string>();

            if ((classifications & PlaneClassifications.Floor) != 0) types.Add("FLOOR");
            if ((classifications & PlaneClassifications.Table) != 0) types.Add("TABLE");
            if ((classifications & PlaneClassifications.Seat) != 0) types.Add("SEAT");
            if ((classifications & PlaneClassifications.Couch) != 0) types.Add("COUCH");
            if ((classifications & PlaneClassifications.WallFace) != 0) types.Add("WALL");
            if ((classifications & PlaneClassifications.Ceiling) != 0) types.Add("CEILING");
            if ((classifications & PlaneClassifications.Other) != 0) types.Add("OTHER");

            if (types.Count > 0)
                return $"{string.Join("+", types)}\n{area:F2}m²";
        }

        // Fallback to alignment
        string alignmentText = plane.alignment == PlaneAlignment.HorizontalUp ? "FLOOR" :
                              plane.alignment == PlaneAlignment.Vertical ? "WALL" : "CEILING";

        return $"{alignmentText}\n{area:F2}m²";
    }
}