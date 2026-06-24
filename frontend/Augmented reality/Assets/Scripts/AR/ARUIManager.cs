using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ARUIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button backButton;

    [Header("Tutorial")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public float tutorialDuration = 6f;

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        // Keep the tutorial panel visible — SurfacePlacementManager controls the text content
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    void OnBackClicked()
    {
        // Go back to the Catalogue page without changing scenes!
        if (AppUIManager.Instance != null)
        {
            AppUIManager.Instance.ShowCatalogue();
        }
        else
        {
            Debug.LogError("AppUIManager is missing!");
        }
    }
}
