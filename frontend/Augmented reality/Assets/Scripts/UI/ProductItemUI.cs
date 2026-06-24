using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;
using System.Threading.Tasks;
using TMPro;

public class ProductItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image thumbnailImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    
    [Header("Buttons")]
    public Button viewInARButton;
    public Button cardButton; // The entire card acts as a button

    private ProductData productData;

    private string pendingThumbnailUrl;
    private bool thumbnailLoaded = false;
    private bool isDownloading = false;

    public void Setup(ProductData data)
    {
        productData = data;
        thumbnailLoaded = false;
        isDownloading = false;
        
        // Populate Texts
        if (nameText != null) nameText.text = data.name;
        if (priceText != null)
        {
            string symbol = string.IsNullOrEmpty(data.currencySymbol) ? "PKR" : data.currencySymbol;
            priceText.text = $"{symbol} {data.price}";
        }

        // Ensure consistent image rendering and clear old recycled image
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = null; // Prevent showing the previous product's image while loading
            thumbnailImage.preserveAspect = true;
        }
        
        // Queue the thumbnail for download
        pendingThumbnailUrl = data.thumbnailUrl;
        TryStartThumbnailDownload();

        // Setup AR Button
        if (viewInARButton != null)
        {
            viewInARButton.onClick.RemoveAllListeners();
            viewInARButton.onClick.AddListener(OnViewInARClicked);
        }

        // Setup Card Button (for details)
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    void OnEnable()
    {
        // Retry thumbnail download if it was queued while the object was inactive
        TryStartThumbnailDownload();
    }

    private void TryStartThumbnailDownload()
    {
        if (thumbnailLoaded || productData == null || isDownloading) return;
        if (!gameObject.activeInHierarchy) return;

        // Try loading from memory/disk cache asynchronously, falling back to network download
        if (!string.IsNullOrEmpty(pendingThumbnailUrl))
        {
            isDownloading = true;
            int targetProductId = productData.id;
            string targetUrl = pendingThumbnailUrl;
            
            StartCoroutine(ImageCache.GetImage(targetProductId, targetUrl, (sprite) =>
            {
                // Verify the card hasn't been recycled/reassigned to a different product
                if (productData != null && productData.id == targetProductId && thumbnailImage != null)
                {
                    isDownloading = false;
                    if (sprite != null)
                    {
                        thumbnailImage.sprite = sprite;
                        thumbnailLoaded = true;
                        pendingThumbnailUrl = null;
                    }
                }
            }));
        }
    }

    void OnDisable()
    {
        // When disabled, any active Coroutine on this object dies instantly.
        // We reset the downloading flag so that OnEnable can safely restart the fetch.
        isDownloading = false;
    }

    void OnCardClicked()
    {
        float clickTime = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {clickTime:F4}s) --- OnCardClicked started for: '{productData.name}' (IsTexture: {productData.IsTexture}) ---");

        if (SelectedProductHolder.Instance != null)
        {
            SelectedProductHolder.Instance.SetSelectedProductData(productData);
        }

        if (AppUIManager.Instance != null)
        {
            AppUIManager.Instance.ShowDetails(productData);
        }
        else
        {
            // We are in the Homepage scene (no AppUIManager present).
            // Track routing history and load the specific product/texture scene.
            if (SelectedProductHolder.Instance != null)
            {
                SelectedProductHolder.Instance.cameFromHomepage = true;
            }
            string targetScene = productData.IsTexture ? "ProductTextureScene" : "ProductModelScene";
            Debug.Log($"Homepage card clicked: '{productData.name}'. Loading scene '{targetScene}' and setting cameFromHomepage.");
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.LoadScene(targetScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
        }
    }

    void OnViewInARClicked()
    {
        if (AppUIManager.Instance != null)
        {
            if (productData != null && productData.IsTexture)
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
            Debug.LogError("ProductItemUI: AppUIManager.Instance is missing. AR mode can only be launched inside the Product Catalog/AR scenes!");
        }
    }
}
