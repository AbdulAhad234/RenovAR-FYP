using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GoToCartButton : MonoBehaviour
{
    [Tooltip("The exact name of the Cart Scene to navigate to.")]
    public string cartSceneName = "CartScene"; // Adjust this if your scene name differs

    private void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnCartButtonClicked);
    }

    private void OnCartButtonClicked()
    {
        Debug.Log("GoToCartButton clicked. Navigating to Cart Scene...");
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(cartSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(cartSceneName);
        }
    }
}
