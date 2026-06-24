using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Shared utility for downloading and caching product images to disk and memory.
/// Images are stored in Application.persistentDataPath/image_cache/.
/// </summary>
public static class ImageCache
{
    private static string CacheDir => Path.Combine(Application.persistentDataPath, "image_cache");

    // In-memory cache to prevent redundant disk reading and texture decoding
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// Clears the in-memory cache of sprites.
    /// </summary>
    public static void ClearMemoryCache()
    {
        spriteCache.Clear();
        Debug.Log("ImageCache: In-memory sprite cache cleared.");
    }

    private static string GetCleanUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        int queryIndex = url.IndexOf('?');
        if (queryIndex >= 0)
        {
            // If the query parameters contain version or timestamp hints, we keep the query string to support cache busting!
            string query = url.Substring(queryIndex + 1).ToLower();
            if (query.Contains("v=") || query.Contains("version") || query.Contains("t=") || 
                query.Contains("time") || query.Contains("timestamp") || query.Contains("update") ||
                query.Contains("hash") || query.Contains("token"))
            {
                return url;
            }
            return url.Substring(0, queryIndex);
        }
        return url;
    }

    /// <summary>
    /// Returns the local cache path for a given product thumbnail.
    /// </summary>
    public static string GetCachePath(int productId, string url)
    {
        string cleanUrl = GetCleanUrl(url);
        string hash = string.IsNullOrEmpty(cleanUrl) ? "default" : cleanUrl.GetHashCode().ToString("X");
        return Path.Combine(CacheDir, $"product_{productId}_{hash}.png");
    }

    /// <summary>
    /// Checks if a cached image exists on disk for this product.
    /// </summary>
    public static bool HasCachedImage(int productId, string url)
    {
        return File.Exists(GetCachePath(productId, url));
    }

    /// <summary>
    /// Loads a cached image from disk and returns it as a Sprite.
    /// Fast check if already in memory; otherwise does a blocking disk load (use GetImage instead when possible).
    /// </summary>
    public static Sprite LoadCachedImage(int productId, string url)
    {
        string path = GetCachePath(productId, url);
        if (spriteCache.TryGetValue(path, out Sprite cached))
        {
            return cached;
        }

        if (!File.Exists(path)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            if (texture.LoadImage(bytes))
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                spriteCache[path] = sprite;
                return sprite;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ImageCache: Failed to load cached image for product {productId}: {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// Unified non-blocking image fetcher.
    /// Tries in-memory cache first (0 frames wait), then asynchronously loads from local disk (if cached),
    /// and falls back to network download if not cached.
    /// </summary>
    public static IEnumerator GetImage(int productId, string url, System.Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        string path = GetCachePath(productId, url);

        // 1. Check in-memory cache
        if (spriteCache.TryGetValue(path, out Sprite cached))
        {
            onComplete?.Invoke(cached);
            yield break;
        }

        // 2. Check disk cache
        if (File.Exists(path))
        {
            // Load file asynchronously using file:// scheme
            using (UnityWebRequest request = UnityWebRequest.Get("file://" + path))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = request.downloadHandler.data;
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.filterMode = FilterMode.Bilinear;
                    
                    if (texture.LoadImage(data))
                    {
                        Texture2D processedTexture = ProcessDownloadedTexture(texture);
                        if (processedTexture != texture)
                        {
                            UnityEngine.Object.Destroy(texture);
                        }
                        
                        Sprite sprite = Sprite.Create(processedTexture, new Rect(0, 0, processedTexture.width, processedTexture.height), new Vector2(0.5f, 0.5f));
                        spriteCache[path] = sprite;
                        onComplete?.Invoke(sprite);
                    }
                    else
                    {
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogWarning($"ImageCache: Failed to load disk cached image asynchronously from {path}: {request.error}");
                    // Fallback to network just in case the file on disk is corrupt or failed to load
                    yield return DownloadAndCache(url, productId, onComplete);
                }
            }
            yield break;
        }

        // 3. Download from network and cache
        yield return DownloadAndCache(url, productId, onComplete);
    }

    /// <summary>
    /// Downloads an image from a URL, caches it to disk and memory, and returns the Sprite via callback.
    /// Must be called from a MonoBehaviour via StartCoroutine.
    /// </summary>
    public static IEnumerator DownloadAndCache(string url, int productId, System.Action<Sprite> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            ulong lastBytes = 0;
            float lastTime = Time.time;
            string speedText = "0 MB/s";

            while (!operation.isDone)
            {
                float progress = request.downloadProgress;
                ulong downloadedBytes = request.downloadedBytes;

                // Smooth download speed update every 0.25 seconds
                if (Time.time - lastTime >= 0.25f)
                {
                    ulong bytesSinceLast = downloadedBytes - lastBytes;
                    float speedBps = bytesSinceLast / (Time.time - lastTime);
                    float speedMbps = speedBps / (1024f * 1024f);
                    speedText = speedMbps.ToString("F1") + " MB/s";

                    lastBytes = downloadedBytes;
                    lastTime = Time.time;
                }

                ulong totalBytes = 0;
                if (progress > 0)
                {
                    totalBytes = (ulong)(downloadedBytes / progress);
                }

                if (AppUIManager.Instance != null && AppUIManager.Instance.activeLoadingPanel != null)
                {
                    AppUIManager.Instance.activeLoadingPanel.UpdateProgress(progress, downloadedBytes, totalBytes, speedText);
                }

                yield return null;
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning($"ImageCache: Failed to download image for product {productId}: {request.error}");
                onComplete?.Invoke(null);
            }
            else
            {
                byte[] data = request.downloadHandler.data;
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                
                if (!texture.LoadImage(data))
                {
                    onComplete?.Invoke(null);
                    yield break;
                }
                
                // Process texture to enforce max size and multiples of 4
                Texture2D processedTexture = ProcessDownloadedTexture(texture);
                if (processedTexture != texture)
                {
                    UnityEngine.Object.Destroy(texture); // Free the original uncompressed massive texture
                }
                
                Sprite sprite = Sprite.Create(processedTexture, new Rect(0, 0, processedTexture.width, processedTexture.height), new Vector2(0.5f, 0.5f));

                // Save to disk cache using the raw downloaded bytes (fastest, saves disk space, and avoids compression errors)
                try
                {
                    if (!Directory.Exists(CacheDir))
                    {
                        Directory.CreateDirectory(CacheDir);
                    }

                    string currentCachePath = GetCachePath(productId, url);
                    
                    // Clean up any old cached images for this product ID
                    if (Directory.Exists(CacheDir))
                    {
                        string[] oldFiles = Directory.GetFiles(CacheDir, $"product_{productId}_*.png");
                        foreach (var oldFile in oldFiles)
                        {
                            try
                            {
                                if (oldFile != currentCachePath)
                                {
                                    File.Delete(oldFile);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"ImageCache: Failed to delete old cached file {oldFile}: {ex.Message}");
                            }
                        }
                    }

                    File.WriteAllBytes(currentCachePath, data);

                    // Add to instant memory cache
                    spriteCache[currentCachePath] = sprite;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"ImageCache: Failed to save image to disk for product {productId}: {e.Message}");
                }

                onComplete?.Invoke(sprite);
            }
        }
    }

    private static Texture2D ProcessDownloadedTexture(Texture2D source)
    {
        int maxDim = 1024;
        int targetWidth = source.width;
        int targetHeight = source.height;

        // Scale down if too large
        if (targetWidth > maxDim || targetHeight > maxDim)
        {
            float ratio = Mathf.Min((float)maxDim / targetWidth, (float)maxDim / targetHeight);
            targetWidth = Mathf.RoundToInt(targetWidth * ratio);
            targetHeight = Mathf.RoundToInt(targetHeight * ratio);
        }

        // Force multiples of 4 for compression
        targetWidth = Mathf.Max(4, (targetWidth / 4) * 4);
        targetHeight = Mathf.Max(4, (targetHeight / 4) * 4);

        // If the original texture is already correct, just compress and return
        if (targetWidth == source.width && targetHeight == source.height)
        {
            source.Compress(false);
            return source;
        }

        // Use RenderTexture to quickly scale on the GPU
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;

        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply(false, false);
        result.Compress(false);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}
