using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class ARSelectionVisualizer : MonoBehaviour
{
    public Material outlineMaterial;
    
    private XRGrabInteractable grabInteractable;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Renderer[] childRenderers;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        // Cache original materials
        childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in childRenderers)
        {
            if (r != null)
            {
                originalMaterials[r] = r.materials;
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (outlineMaterial == null) return;
        
        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;
            if (r != null)
            {
                Material[] mats = new Material[kvp.Value.Length + 1];
                for (int i = 0; i < kvp.Value.Length; i++) mats[i] = kvp.Value[i];
                mats[mats.Length - 1] = outlineMaterial;
                r.materials = mats;
            }
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;
            if (r != null)
            {
                r.materials = kvp.Value;
            }
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }
}
