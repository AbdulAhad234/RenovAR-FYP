using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the Homepage layout.
/// - Top: Horizontal Carousel showing 10 randomized products, auto-scrolling smoothly.
/// - Bottom: Horizontal Scroll view showing 10 different products (default) or search results.
/// - Top Search Bar: Dynamically filters products in the bottom horizontal list by name, description, brand, or vendor.
/// </summary>
public class HomepageManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField searchBar;
    public GameObject productCardPrefab;

    [Header("Featured Carousel (Horizontal)")]
    public ScrollRect featuredScrollRect;
    public Transform featuredGridParent;
    public float autoScrollInterval = 3f;
    public float autoScrollSpeed = 0.5f;

    [Header("Hot Topics (Horizontal)")]
    public ScrollRect hotTopicsScrollRect;
    public Transform hotTopicsGridParent;

    [Header("Section Titles")]
    public TMP_Text hotTopicsTitleText;


    [Header("Navigation Buttons")]
    public Button furnitureSceneButton;
    public string furnitureSceneName;

    public Button materialsSceneButton;
    public string materialsSceneName;

    // Cache copy of all products received
    private ProductData[] allProducts = new ProductData[0];
    private List<ProductData> featuredProducts = new List<ProductData>();
    private List<ProductData> defaultHotTopicsProducts = new List<ProductData>();

    // Scrolling state
    private int currentScrollIndex = 0;
    private Coroutine autoScrollCoroutine;

    void Start()
    {
        // Clear selected product so that entering AR scenes from home doesn't auto-open details of old product
        if (SelectedProductHolder.Instance != null)
        {
            SelectedProductHolder.Instance.SetSelectedProductData(null);
            SelectedProductHolder.Instance.cameFromHomepage = false;
        }

        // Bind search input listener
        if (searchBar != null)
        {
            searchBar.onValueChanged.AddListener(OnSearchValueChanged);
        }

        // Navigation buttons
        if (furnitureSceneButton != null)
        {
            furnitureSceneButton.onClick.AddListener(() =>
            {
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.LoadScene(furnitureSceneName);
                }
                else
                {
                    SceneManager.LoadScene(furnitureSceneName);
                }
            });
        }

        if (materialsSceneButton != null)
        {
            materialsSceneButton.onClick.AddListener(() =>
            {
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.LoadScene(materialsSceneName);
                }
                else
                {
                    SceneManager.LoadScene(materialsSceneName);
                }
            });
        }

        // Start fetching data
        StartCoroutine(WaitForAPIManagerAndFetch());

        if (hotTopicsTitleText != null)
        {
            hotTopicsTitleText.text = "Hot Topics";
        }
    }

    void OnEnable()
    {
        // Subscribe to background update event from APIManager
        if (APIManager.Instance != null)
        {
            APIManager.Instance.OnProductsUpdated -= OnProductsCacheUpdated;
            APIManager.Instance.OnProductsUpdated += OnProductsCacheUpdated;
        }

        // Restart autoscroll if returning to page
        if (featuredProducts.Count > 0)
        {
            StartAutoScroll();
        }
    }

    void OnDisable()
    {
        // Unsubscribe from background update event
        if (APIManager.Instance != null)
        {
            APIManager.Instance.OnProductsUpdated -= OnProductsCacheUpdated;
        }

        StopAutoScroll();
    }

    void OnDestroy()
    {
        if (APIManager.Instance != null)
        {
            APIManager.Instance.OnProductsUpdated -= OnProductsCacheUpdated;
        }
    }

    private IEnumerator WaitForAPIManagerAndFetch()
    {
        // Wait until APIManager.Instance is fully initialized
        while (APIManager.Instance == null)
        {
            yield return null;
        }

        // Ensure subscription
        APIManager.Instance.OnProductsUpdated -= OnProductsCacheUpdated;
        APIManager.Instance.OnProductsUpdated += OnProductsCacheUpdated;

        Debug.Log("HomepageManager: APIManager initialized. Fetching products...");
        APIManager.Instance.GetMarketplaceProducts(OnProductsReceived, OnError);
    }

    private void OnProductsReceived(ProductData[] products)
    {
        if (products == null || products.Length == 0)
        {
            Debug.LogWarning("HomepageManager: Received empty product list.");
            return;
        }

        allProducts = products;
        InitializeHomepageLists();
    }

    private void OnProductsCacheUpdated(ProductData[] products)
    {
        Debug.Log("HomepageManager: Background cache updated. Refreshing data...");
        allProducts = products;
        InitializeHomepageLists();
    }

    private void InitializeHomepageLists()
    {
        if (allProducts.Length == 0) return;

        // 1. Generate 10 randomized products for the Featured Horizontal Carousel
        System.Random rand = new System.Random();
        featuredProducts = allProducts.OrderBy(x => rand.Next()).Take(10).ToList();

        // 2. Select 10 different products for Hot Topics (default vertical view)
        // Ensure no overlap with featured products if possible
        var remainingPool = allProducts.Where(p => !featuredProducts.Any(fp => fp.id == p.id)).ToList();
        
        // If remaining pool is empty (less than 10 products overall), fallback to all products
        if (remainingPool.Count == 0)
        {
            remainingPool = allProducts.ToList();
        }
        
        defaultHotTopicsProducts = remainingPool.OrderBy(x => rand.Next()).Take(10).ToList();

        // 3. Build horizontal featured items
        PopulateGrid(featuredGridParent, featuredProducts);

        // 4. Build vertical hot topics items (or filtered items if search is active)
        if (searchBar != null && !string.IsNullOrEmpty(searchBar.text))
        {
            FilterAndDisplay(searchBar.text);
        }
        else
        {
            PopulateGrid(hotTopicsGridParent, defaultHotTopicsProducts);
        }

        // 5. Start Horizontal AutoScroll slideshow
        StartAutoScroll();
    }

    private void PopulateGrid(Transform gridParent, List<ProductData> products)
    {
        if (gridParent == null) return;

        // Clear existing items
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Spawn new cards
        foreach (var product in products)
        {
            if (productCardPrefab == null) continue;

            GameObject card = Instantiate(productCardPrefab, gridParent);
            card.SetActive(true);
            ProductItemUI ui = card.GetComponent<ProductItemUI>();
            if (ui != null)
            {
                ui.Setup(product);
            }
        }
    }

    private void OnSearchValueChanged(string query)
    {
        FilterAndDisplay(query);
    }

    private void FilterAndDisplay(string query)
    {
        if (allProducts == null || allProducts.Length == 0)
            return;

        // Search box empty
        if (string.IsNullOrWhiteSpace(query))
        {
            if (hotTopicsTitleText != null)
            {
                hotTopicsTitleText.text = "Hot Topics";
            }

            PopulateGrid(hotTopicsGridParent, defaultHotTopicsProducts);
            return;
        }

        string lowerQuery = query.ToLower().Trim();

        List<ProductData> filtered = allProducts.Where(p =>
        {
            bool nameMatch = p.name != null &&
                             p.name.ToLower().Contains(lowerQuery);

            bool descMatch = p.description != null &&
                             p.description.ToLower().Contains(lowerQuery);

            bool brandMatch = p.brandName != null &&
                              p.brandName.ToLower().Contains(lowerQuery);

            bool vendorMatch = p.vendorName != null &&
                               p.vendorName.ToLower().Contains(lowerQuery);

            bool categoryMatch = p.categoryName != null &&
                                 p.categoryName.ToLower().Contains(lowerQuery);

            return nameMatch ||
                   descMatch ||
                   brandMatch ||
                   vendorMatch ||
                   categoryMatch;
        }).ToList();

        if (hotTopicsTitleText != null)
        {
            hotTopicsTitleText.text = $"Search Results for '{query}'";
        }

        Debug.Log($"HomepageManager: Filtered search query '{query}' returned {filtered.Count} products.");

        PopulateGrid(hotTopicsGridParent, filtered);
    }

    // ==========================================
    // Horizontal Carousel AutoScroll Slideshow
    // ==========================================

    private void StartAutoScroll()
    {
        StopAutoScroll();

        if (featuredScrollRect != null && featuredProducts.Count > 1)
        {
            autoScrollCoroutine = StartCoroutine(AutoScrollSlideshow());
        }
    }

    private void StopAutoScroll()
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
            autoScrollCoroutine = null;
        }
    }

    private IEnumerator AutoScrollSlideshow()
    {
        currentScrollIndex = 0;
        if (featuredScrollRect != null)
        {
            featuredScrollRect.horizontalNormalizedPosition = 0f;
        }

        while (true)
        {
            yield return new WaitForSeconds(autoScrollInterval);

            // If user is dragging or touching the screen, wait and skip this tick
            if (Input.GetMouseButton(0))
            {
                yield return new WaitUntil(() => !Input.GetMouseButton(0));
                yield return new WaitForSeconds(1f); // Grace period after letting go
                continue;
            }

            if (featuredProducts.Count <= 1) yield break;

            currentScrollIndex = (currentScrollIndex + 1) % featuredProducts.Count;
            float targetPos = (float)currentScrollIndex / (featuredProducts.Count - 1);

            // Smooth Lerp scroll transition
            float elapsed = 0f;
            float startPos = featuredScrollRect != null ? featuredScrollRect.horizontalNormalizedPosition : 0f;

            while (elapsed < autoScrollSpeed)
            {
                if (Input.GetMouseButton(0))
                {
                    // Interrupted by user drag/touch
                    break;
                }

                elapsed += Time.deltaTime;
                if (featuredScrollRect != null)
                {
                    featuredScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsed / autoScrollSpeed);
                }
                yield return null;
            }

            if (featuredScrollRect != null && !Input.GetMouseButton(0))
            {
                featuredScrollRect.horizontalNormalizedPosition = targetPos;
            }
        }
    }




    private void OnError(string errorMsg)
    {
        Debug.LogError($"HomepageManager: Failed to fetch products: {errorMsg}");
    }
}
