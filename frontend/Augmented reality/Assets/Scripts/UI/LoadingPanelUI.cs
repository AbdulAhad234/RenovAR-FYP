using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingPanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider progressBar;
    public TextMeshProUGUI percentageText;
    public TextMeshProUGUI downloadStatsText;
    public TextMeshProUGUI randomFactText;
    
    [Header("Loading Images")]
    public Image loadingImage;
    public Sprite[] loadingSprites;

    [Header("Settings")]
    public float factCycleTime = 3f;
    public float imageCycleTime = 2f;

    private string[] randomFacts = new string[]
    {
        "Did you know? You can preview furniture in your actual room before buying it — no more guesswork.",
        "Fun fact: RenovAR helps you avoid costly mistakes by showing real scale and placement instantly.",
        "Did you know? You don’t need an interior designer to visualize your space anymore.",
        "Quick tip: What you see in AR is aligned to real-world surfaces like your floor — not random placement.",
        "Did you know? RenovAR is designed to support local furniture markets like “Nilaam Ghar”.",
        "Fun fact: Every product you see can be connected to real vendor listings and prices.",
        "Did you know? You can place, move, rotate, and scale furniture in real time using simple gestures.",
        "Quick insight: RenovAR combines shopping and visualization into one seamless experience.",
        "Did you know? The system is built to scale — new furniture can be added dynamically from a backend.",
        "Fun fact: This isn’t just AR — it’s a step toward smarter, more confident home design decisions."
    };

    private Coroutine factCoroutine;
    private Coroutine imageCoroutine;

    void OnEnable()
    {
        // Reset UI when opened
        if (progressBar != null) progressBar.value = 0f;
        if (percentageText != null) percentageText.text = "0%";
        if (downloadStatsText != null) downloadStatsText.text = "Starting download...";
        
        if (randomFacts.Length > 0) factCoroutine = StartCoroutine(CycleRandomFacts());
        if (loadingSprites != null && loadingSprites.Length > 0) imageCoroutine = StartCoroutine(CycleImages());
    }

    void OnDisable()
    {
        if (factCoroutine != null)
        {
            StopCoroutine(factCoroutine);
            factCoroutine = null;
        }
        if (imageCoroutine != null)
        {
            StopCoroutine(imageCoroutine);
            imageCoroutine = null;
        }
    }

    private IEnumerator CycleRandomFacts()
    {
        while (true)
        {
            if (randomFactText != null)
            {
                int index = Random.Range(0, randomFacts.Length);
                randomFactText.text = randomFacts[index];
            }
            yield return new WaitForSeconds(factCycleTime);
        }
    }

    private IEnumerator CycleImages()
    {
        int currentIndex = 0;
        while (true)
        {
            if (loadingImage != null)
            {
                loadingImage.sprite = loadingSprites[currentIndex];
                currentIndex = (currentIndex + 1) % loadingSprites.Length;
            }
            yield return new WaitForSeconds(imageCycleTime);
        }
    }

    public void UpdateProgress(float progress, ulong downloadedBytes, ulong totalBytes, string speed)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (percentageText != null)
        {
            percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }

        if (downloadStatsText != null)
        {
            float downloadedMB = downloadedBytes / (1024f * 1024f);
            
            if (totalBytes > 0)
            {
                float totalMB = totalBytes / (1024f * 1024f);
                downloadStatsText.text = $"{downloadedMB:F1} MB / {totalMB:F1} MB  -  {speed}";
            }
            else
            {
                // If total size is unknown
                downloadStatsText.text = $"{downloadedMB:F1} MB downloaded  -  {speed}";
            }
        }
    }
}
