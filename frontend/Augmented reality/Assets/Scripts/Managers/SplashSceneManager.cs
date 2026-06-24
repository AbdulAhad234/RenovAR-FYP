using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Attach this to a GameObject in your SplashScene.
/// It pre-fetches products from the API (or loads from disk cache),
/// then async-loads the next scene.
/// </summary>
public class SplashSceneManager : MonoBehaviour
{
    [Header("Navigation Options")]
    [Tooltip("The scene to load if the user is not logged in.")]
    public string loginSceneName = "LoginScene";
    
    [Tooltip("The scene to load if the user is logged in.")]
    public string mainSceneName = "HomepageScene";

    [Tooltip("If true, bypasses the login screen and goes straight to the main scene on startup.")]
    public bool bypassLogin = false;

    [Tooltip("An optional UI Toggle on the Splash Scene to let the user check/uncheck bypass login.")]
    public Toggle bypassLoginToggle;

    [Tooltip("A list of other app scenes for reference or customized loading logic.")]
    public List<string> appScenes = new List<string>() { "LoginScene", "HomepageScene", "ProductCatalogue", "AddToCartScene" };

    [Header("UI (Optional)")]
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI factsText;

    [Header("Timing")]
    public float minimumSplashDuration = 2f; // Minimum time to show the splash screen
    public float factCycleTime = 3f;

    private string[] randomFacts = new string[]
    {
        "Did you know? You can preview furniture in your actual room before buying it — no more guesswork.",
        "Fun fact: RenovAR helps you avoid costly mistakes by showing real scale and placement instantly.",
        "Did you know? You don't need an interior designer to visualize your space anymore.",
        "Quick tip: What you see in AR is aligned to real-world surfaces like your floor — not random placement.",
        "Did you know? RenovAR is designed to support local furniture markets like \u201CNilaam Ghar\u201D.",
        "Fun fact: Every product you see can be connected to real vendor listings and prices.",
        "Did you know? You can place, move, rotate, and scale furniture in real time using simple gestures.",
        "Quick insight: RenovAR combines shopping and visualization into one seamless experience.",
        "Did you know? The system is built to scale — new furniture can be added dynamically from a backend.",
        "Fun fact: This isn't just AR — it's a step toward smarter, more confident home design decisions."
    };

    private bool productsFetched = false;
    private bool productsError = false;
    private float elapsedTime = 0f;
    private Coroutine factCoroutine;

    void Start()
    {
        if (progressBar != null) progressBar.value = 0f;
        SetStatus("Loading...");

        // Initialize bypass toggle if assigned
        if (bypassLoginToggle != null)
        {
            bool savedBypass = PlayerPrefs.GetInt("BypassLoginEnabled", bypassLogin ? 1 : 0) == 1;
            bypassLoginToggle.isOn = savedBypass;
            bypassLoginToggle.onValueChanged.AddListener(OnBypassToggleChanged);
        }

        // Start cycling random facts
        factCoroutine = StartCoroutine(CycleRandomFacts());

        // Start pre-fetching products in the background
        StartCoroutine(PreFetchAndLoad());
    }

    private void OnBypassToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("BypassLoginEnabled", isOn ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"SplashSceneManager: Bypass Login set to {isOn}");
    }

    private IEnumerator PreFetchAndLoad()
    {
        // Step 1: Wait for APIManager to be ready (it uses DontDestroyOnLoad)
        SetStatus("Initializing...");
        float waitStart = Time.time;
        while (APIManager.Instance == null)
        {
            if (Time.time - waitStart > 5f)
            {
                Debug.LogError("SplashSceneManager: APIManager never initialized. Proceeding without products.");
                break;
            }
            yield return null;
        }

        if (progressBar != null) progressBar.value = 0.1f;

        // Step 2: Pre-fetch products (APIManager will use disk cache if available)
        if (APIManager.Instance != null)
        {
            SetStatus("Loading products...");
            productsFetched = false;
            productsError = false;

            APIManager.Instance.GetMarketplaceProducts(
                onSuccess: (products) =>
                {
                    Debug.Log($"SplashScene: Pre-fetched {products.Length} products.");
                    productsFetched = true;
                },
                onError: (err) =>
                {
                    Debug.LogWarning($"SplashScene: Failed to pre-fetch products: {err}. Will retry in catalogue.");
                    productsError = true;
                }
            );

            // Wait for the fetch to complete
            while (!productsFetched && !productsError)
            {
                elapsedTime += Time.deltaTime;
                if (progressBar != null) progressBar.value = Mathf.Lerp(0.1f, 0.5f, elapsedTime / 5f);
                yield return null;
            }
        }

        if (progressBar != null) progressBar.value = 0.5f;
        SetStatus("Preparing app...");

        // Step 3: Ensure minimum splash duration for a polished feel
        float remaining = minimumSplashDuration - elapsedTime;
        if (remaining > 0f)
        {
            float waited = 0f;
            while (waited < remaining)
            {
                waited += Time.deltaTime;
                if (progressBar != null) progressBar.value = Mathf.Lerp(0.5f, 0.7f, waited / remaining);
                yield return null;
            }
        }

        // Step 4: Async load the next scene
        SetStatus("Almost ready...");

        string targetScene = loginSceneName;
        
        // Check if user is actually signed in via the real AuthToken
        string savedToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            targetScene = mainSceneName;
            Debug.Log($"SplashSceneManager: Valid auth token found. Automatically loading main scene: {targetScene}");
        }
        else
        {
            targetScene = loginSceneName;
            Debug.Log($"SplashSceneManager: No auth token found. Loading login scene: {targetScene}");
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            if (progressBar != null) progressBar.value = Mathf.Lerp(0.7f, 1f, asyncLoad.progress / 0.9f);
            yield return null;
        }

        if (progressBar != null) progressBar.value = 1f;
        SetStatus("Ready!");

        // Brief pause at 100% so the user sees it complete
        yield return new WaitForSeconds(0.3f);

        asyncLoad.allowSceneActivation = true;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private IEnumerator CycleRandomFacts()
    {
        while (true)
        {
            if (factsText != null && randomFacts.Length > 0)
            {
                int index = Random.Range(0, randomFacts.Length);
                factsText.text = randomFacts[index];
            }
            yield return new WaitForSeconds(factCycleTime);
        }
    }
}
