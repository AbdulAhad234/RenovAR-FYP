using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CartUIController : MonoBehaviour
{
    [Header("Layout")]
    public Transform contentParent;
    public GameObject cartItemPrefab;

    [Header("Totals")]
    public TextMeshProUGUI subtotalText;
    public TextMeshProUGUI totalText;
    public TextMeshProUGUI bottomTotalText; // The one below the line in the design

    [Header("Buttons")]
    public Button buyButton;
    public Button backButton;

    void OnEnable()
    {
        RefreshUI();
        if (CartManager.Instance != null)
            CartManager.Instance.OnCartUpdated += RefreshUI;
            
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.GoBack();
                }
                else
                {
                    UnityEngine.XR.ARFoundation.LoaderUtility.Initialize();
                    if (NavigationManager.Instance != null)
                    {
                        NavigationManager.Instance.LoadScene("ProductModelScene");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("ProductModelScene");
                    }
                }
            });
        }
    }

    void OnDisable()
    {
        if (CartManager.Instance != null)
            CartManager.Instance.OnCartUpdated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (CartManager.Instance == null) 
        {
            Debug.LogWarning("CartUIController: CartManager.Instance is null!");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("CartUIController: contentParent is not assigned in the Inspector!");
            return;
        }

        if (cartItemPrefab == null)
        {
            Debug.LogError("CartUIController: cartItemPrefab is not assigned in the Inspector!");
            return;
        }

        // Clear existing items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Populate items
        List<CartItem> items = CartManager.Instance.GetItems();
        Debug.Log($"CartUIController: Refreshing UI with {items.Count} items.");

        foreach (var item in items)
        {
            GameObject instance = Instantiate(cartItemPrefab, contentParent);
            CartItemUI itemUI = instance.GetComponent<CartItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(item, 
                    (id, selected) => CartManager.Instance.ToggleSelection(id, selected),
                    (id) => CartManager.Instance.RemoveFromCart(id));
            }
            else
            {
                Debug.LogError("CartUIController: The cartItemPrefab is missing the 'CartItemUI' script!");
            }
        }

        // Update Totals
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        float subtotal = CartManager.Instance.CalculateSubtotal();
        float total = CartManager.Instance.CalculateTotal();

        string currency = "PKR"; // Default or get from items
        
        if (subtotalText != null) subtotalText.text = $"{currency} {subtotal:N0}";
        if (totalText != null) totalText.text = $"{currency} {total:N0}";
        if (bottomTotalText != null) bottomTotalText.text = $"{currency} {total:N0}";
    }
}
