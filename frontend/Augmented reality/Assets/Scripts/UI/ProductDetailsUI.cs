using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class ProductDetailsUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image productImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Buttons")]
    public Button addToCartButton;
    public Button viewInARButton;
    public Button backButton;

    private ProductData productData;

    void Start()
    {
        // Wire up back button immediately so it works even if Setup hasn't been called
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => 
            {
                if (AppUIManager.Instance != null)
                    AppUIManager.Instance.ShowCatalogue();
            });
        }
    }

    public void Setup(ProductData data)
    {
        productData = data;

        if (productData == null)
        {
            Debug.LogError("No product data provided to Details UI!");
            return;
        }

        // 2. Populate UI
        if (nameText != null) 
            nameText.text = productData.name;
        else 
            Debug.LogError("ProductDetailsUI: 'nameText' is NOT assigned in the Inspector! Please drag the text object into the script.");

        if (priceText != null)
        {
            string symbol = string.IsNullOrEmpty(productData.currencySymbol) ? "$" : productData.currencySymbol;
            priceText.text = $"{symbol} {productData.price}";
        }
        else 
            Debug.LogError("ProductDetailsUI: 'priceText' is NOT assigned in the Inspector!");

        if (descriptionText != null) 
            descriptionText.text = string.IsNullOrEmpty(productData.description) ? "No description available." : productData.description;
        else 
            Debug.LogError("ProductDetailsUI: 'descriptionText' is NOT assigned in the Inspector!");

        // Ensure consistent image rendering
        if (productImage != null)
        {
            productImage.preserveAspect = true;
        }

        // 3. Load Image asynchronously (Memory -> Async Disk -> Network)
        if (!string.IsNullOrEmpty(productData.thumbnailUrl))
        {
            int targetProductId = productData.id;
            StartCoroutine(ImageCache.GetImage(targetProductId, productData.thumbnailUrl, (sprite) =>
            {
                if (productData != null && productData.id == targetProductId && sprite != null && productImage != null)
                {
                    productImage.sprite = sprite;
                }
            }));
        }

        // 4. Setup Buttons
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => 
            {
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.GoBack();
                }
                else if (AppUIManager.Instance != null)
                {
                    AppUIManager.Instance.ShowCatalogue();
                }
            });
        }

        if (addToCartButton != null)
        {
            addToCartButton.onClick.RemoveAllListeners();
            addToCartButton.onClick.AddListener(OnAddToCartClicked);
        }

        if (viewInARButton != null)
        {
            viewInARButton.onClick.RemoveAllListeners();
            // Show View in AR for both 3D models and textures
            viewInARButton.gameObject.SetActive(productData.Is3DModel || productData.IsTexture);
            
            // Update button label for textures
            TextMeshProUGUI btnText = viewInARButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = productData.IsTexture ? "Preview in AR" : "View in AR";
            }

            viewInARButton.onClick.AddListener(() => 
            {
                if (AppUIManager.Instance != null)
                {
                    if (productData.IsTexture)
                    {
                        AppUIManager.Instance.StartTextureARProcess(productData, viewInARButton);
                    }
                    else
                    {
                        AppUIManager.Instance.StartViewInARProcess(productData, viewInARButton);
                    }
                }
                else
                {
                    Debug.LogError("ProductDetailsUI: AppUIManager.Instance is missing. Cannot view in AR!");
                }
            });
        }
    }

    void OnAddToCartClicked()
    {
        if (productData == null) return;

        Debug.Log($"Added {productData.name} to Cart!");
        
        if (CartManager.Instance != null)
        {
            CartManager.Instance.AddToCart(productData);
        }
    }
}
