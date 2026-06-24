using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BackButton : MonoBehaviour
{
    public enum BackActionType
    {
        GlobalBack,           // Asks NavigationManager to either close the top panel or go back a scene
        CloseSpecificPanel,   // Explicitly deactivates a given panel GameObject
        LoadSpecificScene     // Explicitly loads a given scene name
    }

    [Header("Behavior Settings")]
    [Tooltip("What should this back button do?")]
    public BackActionType actionType = BackActionType.GlobalBack;

    [Tooltip("Assign the UI Panel you want to close (Used if actionType is CloseSpecificPanel)")]
    public GameObject targetPanel;

    [Tooltip("Assign the scene name you want to load (Used if actionType is LoadSpecificScene)")]
    public string targetSceneName;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        switch (actionType)
        {
            case BackActionType.GlobalBack:
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.GoBack();
                }
                else
                {
                    Debug.LogWarning("BackButton: NavigationManager instance not found. Falling back to HomepageScene.");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("HomepageScene");
                }
                break;

            case BackActionType.CloseSpecificPanel:
                if (targetPanel != null)
                {
                    targetPanel.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("BackButton: Target Panel is not assigned!", this);
                }
                break;

            case BackActionType.LoadSpecificScene:
                if (!string.IsNullOrEmpty(targetSceneName))
                {
                    if (NavigationManager.Instance != null)
                    {
                        NavigationManager.Instance.LoadScene(targetSceneName);
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
                    }
                }
                else
                {
                    Debug.LogWarning("BackButton: Target Scene Name is empty!", this);
                }
                break;
        }
    }
}
