using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class ProductGridSpawner : MonoBehaviour
{
    public GameObject productCardPrefab;
    public Transform gridParent;

    void Start()
    {
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) --- ProductGridSpawner Start called ---");
        StartCoroutine(WaitForAPIManagerAndFetch());
    }

    private IEnumerator WaitForAPIManagerAndFetch()
    {
        float waitStart = Time.realtimeSinceStartup;
        // Wait until APIManager.Instance is fully initialized (solves script execution order bugs)
        while (APIManager.Instance == null)
        {
            yield return null;
        }
        float waitDuration = Time.realtimeSinceStartup - waitStart;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) ProductGridSpawner waited {waitDuration:F4}s for APIManager.Instance");

        // Subscribe to background updates to reload if new items/prices come in
        APIManager.Instance.OnProductsUpdated -= OnProductsReceived;
        APIManager.Instance.OnProductsUpdated += OnProductsReceived;

        float apiFetchStart = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {apiFetchStart:F4}s) Calling APIManager.Instance.GetMarketplaceProducts...");
        APIManager.Instance.GetMarketplaceProducts(
            (products) => {
                float fetchDuration = Time.realtimeSinceStartup - apiFetchStart;
                Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) GetMarketplaceProducts callback invoked. Duration: {fetchDuration:F4}s");
                OnProductsReceived(products);
            }, 
            OnError
        );
    }

    void OnDestroy()
    {
        if (APIManager.Instance != null)
        {
            APIManager.Instance.OnProductsUpdated -= OnProductsReceived;
        }
    }

    private ProductData[] baseSceneProducts;

    private void OnProductsReceived(ProductData[] products)
    {
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {start:F4}s) --- OnProductsReceived started with {products.Length} total products ---");
        
        // Filter products based on the current scene name (base list)
        float filterStart = Time.realtimeSinceStartup;
        baseSceneProducts = FilterProductsForScene(products);
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Filtered {baseSceneProducts.Length} products for scene in {Time.realtimeSinceStartup - filterStart:F4}s");

        // Render the base list initially
        RenderProducts(baseSceneProducts);
    }

    public ProductData[] GetBaseProducts()
    {
        return baseSceneProducts ?? new ProductData[0];
    }

    public void RenderProducts(ProductData[] productsToRender)
    {
        float spawnStart = Time.realtimeSinceStartup;
        int childCount = gridParent.childCount;
        int targetCount = productsToRender.Length;

        for (int i = 0; i < Mathf.Max(childCount, targetCount); i++)
        {
            if (i < targetCount)
            {
                // We need a UI card for this product
                GameObject item;
                if (i < childCount)
                {
                    item = gridParent.GetChild(i).gameObject; // Reuse existing
                }
                else
                {
                    item = Instantiate(productCardPrefab, gridParent); // Instantiate new if pool is empty
                }

                item.SetActive(true);
                ProductItemUI ui = item.GetComponent<ProductItemUI>();
                ui.Setup(productsToRender[i]);
            }
            else
            {
                // Deactivate extra children that are no longer needed
                gridParent.GetChild(i).gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Pooled and setup {productsToRender.Length} cards in {Time.realtimeSinceStartup - spawnStart:F4}s");
    }

    /// <summary>
    /// Filters products based on the current scene name:
    /// - Scene contains "model" -> only show 3D model products (modelsCount > 0)
    /// - Scene contains "texture" -> only show texture products (Wall Textures category)
    /// - Otherwise -> show all products
    /// </summary>
    private ProductData[] FilterProductsForScene(ProductData[] allProducts)
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();

        if (sceneName.Contains("texture") || sceneName.Contains("material") || sceneName.Contains("wall"))
        {
            // Only show texture/material products
            return allProducts.Where(p => p.IsTexture).ToArray();
        }
        else if (sceneName.Contains("model") || sceneName.Contains("furniture") || 
                 sceneName.Contains("catalogue") || sceneName.Contains("catalog") ||
                 sceneName.Contains("product"))
        {
            // Only show 3D model products (exclude textures)
            return allProducts.Where(p => p.Is3DModel).ToArray();
        }

        // Default: show everything
        return allProducts;
    }

    private void OnError(string errorMsg)
    {
        Debug.LogError($"Failed to load products: {errorMsg}");
    }
}
