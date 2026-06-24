using UnityEngine;
using TMPro;

public class CartBadgeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject badgeContainer; // The image background for the count
    public TextMeshProUGUI countText;

    void Start()
    {
        UpdateBadge();
        if (CartManager.Instance != null)
        {
            CartManager.Instance.OnCartUpdated += UpdateBadge;
        }
    }

    void OnDestroy()
    {
        if (CartManager.Instance != null)
        {
            CartManager.Instance.OnCartUpdated -= UpdateBadge;
        }
    }

    private void UpdateBadge()
    {
        if (CartManager.Instance == null) return;

        int count = CartManager.Instance.GetCartCount();

        if (count > 0)
        {
            if (badgeContainer != null) badgeContainer.SetActive(true);
            if (countText != null) countText.text = count.ToString();
        }
        else
        {
            if (badgeContainer != null) badgeContainer.SetActive(false);
        }
    }
}
