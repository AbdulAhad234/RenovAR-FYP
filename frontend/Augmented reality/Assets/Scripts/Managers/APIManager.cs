using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.IO;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }

    [Header("API Settings")]
    // Replace this with your actual backend URL (e.g., http://localhost:3000/api or your production URL)
    public string backendBaseUrl = "https://renovar-backend-8fg8.onrender.com/api";

    [Header("Cache Settings")]
    [Tooltip("How long (in hours) before the disk cache expires and a fresh server request is made.")]
    public float cacheExpiryHours = 24f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- In-Memory & Background Cache ---
    private ProductData[] cachedMarketplaceProducts;
    private bool hasFetchedFromServerThisSession = false;
    private bool isFetchingFromServer = false;

    // Callbacks waiting for the first server fetch if no cache is available
    private readonly System.Collections.Generic.List<Action<ProductData[]>> pendingSuccessCallbacks = new System.Collections.Generic.List<Action<ProductData[]>>();
    private readonly System.Collections.Generic.List<Action<string>> pendingErrorCallbacks = new System.Collections.Generic.List<Action<string>>();

    // Event fired whenever the products cache is updated with fresh server data
    public event Action<ProductData[]> OnProductsUpdated;

    // --- Disk Cache Paths ---
    private string ProductsCacheFilePath => Path.Combine(Application.persistentDataPath, "products_cache.json");
    private const string CACHE_TIMESTAMP_KEY = "ProductsCacheTimestamp";

    /// <summary>
    /// Fetches all approved marketplace products.
    /// Priority: In-Memory Cache -> Disk Cache (instant loading) -> Server (background update).
    /// </summary>
    public void GetMarketplaceProducts(Action<ProductData[]> onSuccess, Action<string> onError)
    {
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {start:F4}s) --- GetMarketplaceProducts called ---");
        
        // Case 1: In-memory cache is available and we already fetched from the server this session
        if (cachedMarketplaceProducts != null && cachedMarketplaceProducts.Length > 0 && hasFetchedFromServerThisSession)
        {
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Case 1: Returning fresh in-memory cached marketplace products ({cachedMarketplaceProducts.Length} items).");
            onSuccess?.Invoke(cachedMarketplaceProducts);
            return;
        }

        // Case 2: In-memory cache is available but we haven't fetched from the server this session yet
        if (cachedMarketplaceProducts != null && cachedMarketplaceProducts.Length > 0)
        {
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Case 2: Returning in-memory products ({cachedMarketplaceProducts.Length} items), starting background check...");
            onSuccess?.Invoke(cachedMarketplaceProducts);
            
            if (!isFetchingFromServer)
            {
                StartCoroutine(FetchFromServerInBackground());
            }
            return;
        }

        // Case 3: In-memory cache is null, try disk cache
        float diskStart = Time.realtimeSinceStartup;
        bool loadedFromDisk = TryLoadFromDiskCache(out ProductData[] diskProducts);
        float diskDuration = Time.realtimeSinceStartup - diskStart;
        Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Disk cache check completed in {diskDuration:F4}s. Loaded successfully: {loadedFromDisk}");

        if (loadedFromDisk)
        {
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Case 3: Returning disk-cached products ({diskProducts.Length} items), starting background check...");
            cachedMarketplaceProducts = diskProducts;
            PreCacheAllThumbnails(diskProducts);
            onSuccess?.Invoke(diskProducts);

            if (!isFetchingFromServer)
            {
                StartCoroutine(FetchFromServerInBackground());
            }
        }
        else
        {
            // Case 4: No cache available (first run)
            Debug.Log($"[DIAGNOSTICS] [{System.DateTime.UtcNow:HH:mm:ss.fff}] (Time: {Time.realtimeSinceStartup:F4}s) Case 4: No cache available (first run). Fetching from server (blocking)...");
            Debug.Log("No cache available. Fetching from server (blocking)...");
            
            // Queue the callbacks
            if (onSuccess != null) pendingSuccessCallbacks.Add(onSuccess);
            if (onError != null) pendingErrorCallbacks.Add(onError);

            if (!isFetchingFromServer)
            {
                StartCoroutine(FetchFromServerBlocking());
            }
        }
    }

    private IEnumerator FetchFromServerInBackground()
    {
        isFetchingFromServer = true;
        Debug.Log("Background server fetch started.");

        yield return StartCoroutine(GetRequest($"{backendBaseUrl}/marketplace/products", 
            (products) => {
                isFetchingFromServer = false;
                hasFetchedFromServerThisSession = true;
                
                cachedMarketplaceProducts = products;
                SaveToDiskCache(products);
                PreCacheAllThumbnails(products);
                
                Debug.Log($"Background fetch succeeded. Firing OnProductsUpdated event with {products.Length} products.");
                OnProductsUpdated?.Invoke(products);
            }, 
            (serverError) => {
                isFetchingFromServer = false;
                Debug.LogWarning($"Background server fetch failed: {serverError}");
                // Keep the cached data; do not call onError to avoid disrupting the UI
            }
        ));
    }

    private IEnumerator FetchFromServerBlocking()
    {
        isFetchingFromServer = true;
        Debug.Log("Blocking server fetch started (first launch)...");

        yield return StartCoroutine(GetRequest($"{backendBaseUrl}/marketplace/products", 
            (products) => {
                isFetchingFromServer = false;
                hasFetchedFromServerThisSession = true;
                
                cachedMarketplaceProducts = products;
                SaveToDiskCache(products);
                PreCacheAllThumbnails(products);

                Debug.Log($"Blocking fetch succeeded. Notifying {pendingSuccessCallbacks.Count} pending callbacks.");
                
                // Fire OnProductsUpdated event first
                OnProductsUpdated?.Invoke(products);

                // Invoke and clear pending success callbacks
                var callbacks = new System.Collections.Generic.List<Action<ProductData[]>>(pendingSuccessCallbacks);
                pendingSuccessCallbacks.Clear();
                pendingErrorCallbacks.Clear();

                foreach (var cb in callbacks)
                {
                    try { cb?.Invoke(products); } catch (Exception e) { Debug.LogError(e); }
                }
            }, 
            (serverError) => {
                isFetchingFromServer = false;
                Debug.LogError($"Blocking server fetch failed: {serverError}");

                // Invoke and clear pending error callbacks
                var callbacks = new System.Collections.Generic.List<Action<string>>(pendingErrorCallbacks);
                pendingSuccessCallbacks.Clear();
                pendingErrorCallbacks.Clear();

                foreach (var cb in callbacks)
                {
                    try { cb?.Invoke(serverError); } catch (Exception e) { Debug.LogError(e); }
                }
            }
        ));
    }

    /// <summary>
    /// Force a fresh fetch from the server, ignoring all caches.
    /// Useful for pull-to-refresh.
    /// </summary>
    public void RefreshMarketplaceProducts(Action<ProductData[]> onSuccess, Action<string> onError)
    {
        cachedMarketplaceProducts = null;
        hasFetchedFromServerThisSession = false;
        ClearDiskCache();

        StartCoroutine(GetRequest($"{backendBaseUrl}/marketplace/products", (products) => {
            hasFetchedFromServerThisSession = true;
            cachedMarketplaceProducts = products;
            SaveToDiskCache(products);
            PreCacheAllThumbnails(products);
            OnProductsUpdated?.Invoke(products);
            onSuccess?.Invoke(products);
        }, onError));
    }

    public void ClearProductCache()
    {
        cachedMarketplaceProducts = null;
        hasFetchedFromServerThisSession = false;
        ClearDiskCache();
    }

    /// <summary>
    /// Fetches lightweight AR data for a specific product.
    /// Priority: Server fetch -> Disk Cache fallback.
    /// </summary>
    public void GetARProductData(string productId, Action<ARProductData> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetSingleRequest($"{backendBaseUrl}/ar/products/{productId}", 
            onSuccess: (arData) => {
                SaveARProductDataToCache(productId, arData);
                onSuccess?.Invoke(arData);
            }, 
            onError: (err) => {
                if (TryLoadARProductDataFromCache(productId, out ARProductData cachedData))
                {
                    Debug.Log($"Loaded AR product {productId} data from disk cache.");
                    onSuccess?.Invoke(cachedData);
                }
                else
                {
                    onError?.Invoke(err);
                }
            }
        ));
    }

    private void SaveARProductDataToCache(string productId, ARProductData arData)
    {
        try
        {
            string json = JsonUtility.ToJson(arData);
            string filePath = Path.Combine(Application.persistentDataPath, $"ar_product_{productId}.json");
            File.WriteAllText(filePath, json);
            Debug.Log($"Saved AR product {productId} data to disk cache.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to cache AR product data for {productId}: {e.Message}");
        }
    }

    private bool TryLoadARProductDataFromCache(string productId, out ARProductData arData)
    {
        arData = null;
        string filePath = Path.Combine(Application.persistentDataPath, $"ar_product_{productId}.json");
        if (!File.Exists(filePath)) return false;

        try
        {
            string json = File.ReadAllText(filePath);
            arData = JsonUtility.FromJson<ARProductData>(json);
            return arData != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load cached AR product data for {productId}: {e.Message}");
            return false;
        }
    }

    // ===========================
    // Disk Cache Implementation
    // ===========================

    /// <summary>
    /// Wrapper class so JsonUtility can serialize/deserialize a ProductData array.
    /// </summary>
    [Serializable]
    private class ProductCacheWrapper
    {
        public ProductData[] products;
    }

    private void SaveToDiskCache(ProductData[] products)
    {
        try
        {
            ProductCacheWrapper wrapper = new ProductCacheWrapper { products = products };
            string json = JsonUtility.ToJson(wrapper, false);
            File.WriteAllText(ProductsCacheFilePath, json);

            // Store the timestamp of when we cached
            PlayerPrefs.SetString(CACHE_TIMESTAMP_KEY, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();

            Debug.Log($"Saved {products.Length} products to disk cache at {ProductsCacheFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to save products to disk cache: {e.Message}");
        }
    }

    private bool TryLoadFromDiskCache(out ProductData[] products)
    {
        products = null;

        // Check if cache file exists
        if (!File.Exists(ProductsCacheFilePath))
        {
            Debug.Log("No disk cache file found.");
            return false;
        }

        // Check if cache has expired (make cacheExpiryHours functional)
        if (PlayerPrefs.HasKey(CACHE_TIMESTAMP_KEY))
        {
            string tsStr = PlayerPrefs.GetString(CACHE_TIMESTAMP_KEY);
            if (DateTime.TryParse(tsStr, out DateTime cachedTime))
            {
                double hoursElapsed = (DateTime.UtcNow - cachedTime).TotalHours;
                if (hoursElapsed > cacheExpiryHours)
                {
                    Debug.Log($"Disk cache expired ({hoursElapsed:F1} hours elapsed, limit is {cacheExpiryHours} hours). Clearing...");
                    ClearDiskCache();
                    return false;
                }
            }
        }

        // Try to read and parse
        try
        {
            string json = File.ReadAllText(ProductsCacheFilePath);
            ProductCacheWrapper wrapper = JsonUtility.FromJson<ProductCacheWrapper>(json);

            if (wrapper != null && wrapper.products != null && wrapper.products.Length > 0)
            {
                products = wrapper.products;
                Debug.Log($"Loaded {products.Length} products from disk cache.");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to read disk cache: {e.Message}");
        }

        return false;
    }

    private void PreCacheAllThumbnails(ProductData[] products)
    {
        if (products == null || products.Length == 0) return;
        StartCoroutine(PreCacheThumbnailsRoutine(products));
    }

    private IEnumerator PreCacheThumbnailsRoutine(ProductData[] products)
    {
        Debug.Log($"PreCacheAllThumbnails: Starting background pre-caching of {products.Length} thumbnails...");
        foreach (var product in products)
        {
            if (product == null || string.IsNullOrEmpty(product.thumbnailUrl)) continue;

            // Check if it's already cached
            if (!ImageCache.HasCachedImage(product.id, product.thumbnailUrl))
            {
                Debug.Log($"PreCacheAllThumbnails: Pre-downloading thumbnail for product {product.id} ({product.name})...");
                // Start download and cache, pass null as callback since we just want to write it to disk
                yield return StartCoroutine(ImageCache.DownloadAndCache(product.thumbnailUrl, product.id, null));
            }
        }
        Debug.Log("PreCacheAllThumbnails: Background pre-caching complete!");
    }

    private void ClearDiskCache()
    {
        try
        {
            if (File.Exists(ProductsCacheFilePath))
            {
                File.Delete(ProductsCacheFilePath);
            }
            PlayerPrefs.DeleteKey(CACHE_TIMESTAMP_KEY);
            PlayerPrefs.Save();
            Debug.Log("Disk cache cleared.");

            // Clear the image cache on disk as well to force redownloads
            string imageCacheDir = Path.Combine(Application.persistentDataPath, "image_cache");
            if (Directory.Exists(imageCacheDir))
            {
                Directory.Delete(imageCacheDir, true);
                Debug.Log("Image disk cache cleared.");
            }
            ImageCache.ClearMemoryCache();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to clear disk cache: {e.Message}");
        }
    }

    // ===========================
    // Network Requests
    // ===========================

    /// <summary>
    /// Fetches ALL pages of marketplace products and returns them as a single combined array.
    /// </summary>
    private IEnumerator GetRequest(string baseUrl, Action<ProductData[]> onSuccess, Action<string> onError)
    {
        var allProducts = new System.Collections.Generic.List<ProductData>();
        int currentPage = 1;
        bool hasNext = true;

        while (hasNext)
        {
            string url = $"{baseUrl}?page={currentPage}&limit=20";
            Debug.Log($"Fetching page {currentPage}: {url}");

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.GetToken()}");
                }

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error fetching page {currentPage} from {url}: {webRequest.error}");
                    
                    // If we already have some products from earlier pages, return those
                    if (allProducts.Count > 0)
                    {
                        Debug.LogWarning($"Returning {allProducts.Count} products from {currentPage - 1} pages (page {currentPage} failed).");
                        onSuccess?.Invoke(allProducts.ToArray());
                    }
                    else
                    {
                        onError?.Invoke(webRequest.error);
                    }
                    yield break;
                }

                string jsonResponse = webRequest.downloadHandler.text;

                try
                {
                    MarketplaceResponse response = JsonUtility.FromJson<MarketplaceResponse>(jsonResponse);
                    
                    if (response != null && response.success && response.data != null && response.data.data != null)
                    {
                        allProducts.AddRange(response.data.data);
                        
                        // Check pagination for more pages
                        if (response.data.pagination != null)
                        {
                            hasNext = response.data.pagination.hasNext;
                            Debug.Log($"Page {currentPage}: Got {response.data.data.Length} products. Total so far: {allProducts.Count}/{response.data.pagination.total}. HasNext: {hasNext}");
                        }
                        else
                        {
                            hasNext = false;
                        }
                        
                        currentPage++;
                    }
                    else
                    {
                        if (allProducts.Count > 0)
                        {
                            onSuccess?.Invoke(allProducts.ToArray());
                        }
                        else
                        {
                            onError?.Invoke("Failed to parse JSON or API returned success=false.");
                        }
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON Parsing Error on page {currentPage}: {e.Message}");
                    if (allProducts.Count > 0)
                    {
                        onSuccess?.Invoke(allProducts.ToArray());
                    }
                    else
                    {
                        onError?.Invoke(e.Message);
                    }
                    yield break;
                }
            }
        }

        Debug.Log($"Finished fetching all products. Total: {allProducts.Count}");
        onSuccess?.Invoke(allProducts.ToArray());
    }

    private IEnumerator GetSingleRequest(string url, Action<ARProductData> onSuccess, Action<string> onError)
    {
         using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {AuthManager.Instance.GetToken()}");
            }

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching data from {url}: {webRequest.error}");
                onError?.Invoke(webRequest.error);
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                try
                {
                    ARProductResponse response = JsonUtility.FromJson<ARProductResponse>(jsonResponse);
                    if (response != null && response.success && response.data != null)
                    {
                        onSuccess?.Invoke(response.data);
                    }
                    else
                    {
                        onError?.Invoke("Failed to parse AR product JSON or API returned success=false.");
                    }
                }
                catch (Exception e)
                {
                     onError?.Invoke(e.Message);
                }
            }
        }
    }
}
