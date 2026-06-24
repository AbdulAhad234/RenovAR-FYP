using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages placed AR objects with an object-pooling strategy:
///   - "Delete All" sets objects inactive (pools them) instead of destroying.
///   - If the SAME product is selected again, the pooled object is reused.
///   - If a DIFFERENT product is selected, old pooled objects are destroyed to free memory.
///   - All objects are fully destroyed when the app quits.
/// </summary>
public class PlacedObjectsManager : MonoBehaviour
{
    public static PlacedObjectsManager Instance { get; private set; }

    // Active objects currently visible in the scene
    private List<GameObject> activePlacedObjects = new List<GameObject>();

    // Pooled (deactivated) objects keyed by product ID for quick reuse lookup
    private Dictionary<int, GameObject> pooledObjects = new Dictionary<int, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("PlacedObjectsManager");
            go.AddComponent<PlacedObjectsManager>();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        // Full cleanup on app close
        DestroyAll();
    }

    /// <summary>
    /// Register a newly placed object as active.
    /// </summary>
    public void RegisterPlacedObject(GameObject obj, int productId = -1)
    {
        if (!activePlacedObjects.Contains(obj))
        {
            activePlacedObjects.Add(obj);
            DontDestroyOnLoad(obj);
        }

        // If there was a pooled object for this product ID, it's been reused
        // so remove it from the pool dictionary (it's now active)
        if (productId >= 0 && pooledObjects.ContainsKey(productId))
        {
            if (pooledObjects[productId] == obj)
            {
                pooledObjects.Remove(productId);
            }
        }
    }

    /// <summary>
    /// Unregister a placed object when it is deleted/destroyed.
    /// </summary>
    public void UnregisterPlacedObject(GameObject obj)
    {
        if (activePlacedObjects.Contains(obj))
        {
            activePlacedObjects.Remove(obj);
        }

        // Also clean up from the pooledObjects registry if it was tracked there
        int foundKey = -1;
        foreach (var kvp in pooledObjects)
        {
            if (kvp.Value == obj)
            {
                foundKey = kvp.Key;
                break;
            }
        }
        if (foundKey >= 0)
        {
            pooledObjects.Remove(foundKey);
        }
    }

    /// <summary>
    /// Soft-delete a single object: deactivate it and move it to the pool.
    /// Reuses it if the same product is placed again.
    /// </summary>
    public void PoolPlacedObject(GameObject obj)
    {
        if (obj == null) return;

        if (activePlacedObjects.Contains(obj))
        {
            activePlacedObjects.Remove(obj);
        }

        int productId = ExtractProductId(obj);

        var manipulator = obj.GetComponent<ARStandaloneManipulator>();
        if (manipulator != null)
        {
            manipulator.Deselect();
        }

        obj.SetActive(false);

        // Store in pool — if there's already a pooled object for this product, destroy the old one
        if (productId >= 0)
        {
            if (pooledObjects.ContainsKey(productId) && pooledObjects[productId] != obj)
            {
                if (pooledObjects[productId] != null) Destroy(pooledObjects[productId]);
            }
            pooledObjects[productId] = obj;
        }

        Debug.Log($"PlacedObjectsManager: Pooled single object '{obj.name}'. Pool size: {pooledObjects.Count}");
    }

    /// <summary>
    /// Soft-delete: deactivate all placed objects and move them to the pool.
    /// Reuses them if the same product is placed again.
    /// </summary>
    public void PoolAllObjects()
    {
        for (int i = activePlacedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activePlacedObjects[i];
            if (obj == null)
            {
                activePlacedObjects.RemoveAt(i);
                continue;
            }

            // Extract product ID from the object name (format: "Cached_AR_Product_{id}")
            int productId = ExtractProductId(obj);

            // Deselect if selected
            var manipulator = obj.GetComponent<ARStandaloneManipulator>();
            if (manipulator != null && ARStandaloneManipulator.SelectedObject == manipulator)
            {
                manipulator.Deselect();
            }

            obj.SetActive(false);

            // Store in pool — if there's already a pooled object for this product, destroy the old one
            if (productId >= 0)
            {
                if (pooledObjects.ContainsKey(productId) && pooledObjects[productId] != obj)
                {
                    if (pooledObjects[productId] != null) Destroy(pooledObjects[productId]);
                }
                pooledObjects[productId] = obj;
            }

            activePlacedObjects.RemoveAt(i);
        }

        Debug.Log($"PlacedObjectsManager: Pooled all objects. Pool size: {pooledObjects.Count}");
    }

    /// <summary>
    /// Tries to retrieve a pooled (inactive) object for a given product ID.
    /// Returns null if no pooled object exists.
    /// </summary>
    public GameObject TryGetPooledObject(int productId)
    {
        if (productId < 0) return null;

        if (pooledObjects.TryGetValue(productId, out GameObject pooledObj))
        {
            if (pooledObj != null)
            {
                pooledObjects.Remove(productId);
                Debug.Log($"PlacedObjectsManager: Reusing pooled object for product {productId}");
                return pooledObj;
            }
            else
            {
                // Object was destroyed externally
                pooledObjects.Remove(productId);
            }
        }
        return null;
    }

    /// <summary>
    /// Called when a NEW (different) product is being loaded for AR.
    /// Destroys all pooled objects that don't match the new product ID
    /// to free memory since they won't be reused.
    /// </summary>
    public void EvictPoolExcept(int keepProductId)
    {
        var toRemove = new List<int>();
        foreach (var kvp in pooledObjects)
        {
            if (kvp.Key != keepProductId)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int key in toRemove)
        {
            pooledObjects.Remove(key);
        }

        if (toRemove.Count > 0)
        {
            Debug.Log($"PlacedObjectsManager: Evicted {toRemove.Count} pooled objects (keeping product {keepProductId})");
        }
    }

    /// <summary>
    /// Hard-delete: destroy everything (active + pooled). Used on app quit.
    /// </summary>
    public void DestroyAll()
    {
        foreach (var obj in activePlacedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        activePlacedObjects.Clear();

        foreach (var kvp in pooledObjects)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        pooledObjects.Clear();
    }

    /// <summary>
    /// Legacy method — now calls PoolAllObjects instead of destroying.
    /// </summary>
    public void ClearAllObjects()
    {
        PoolAllObjects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isARScene = scene.name == "ProductModelScene" || scene.name == "ProductTextureScene";

        // Show/hide active objects based on scene
        foreach (var obj in activePlacedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isARScene);
            }
        }

        // Pooled objects always stay inactive
    }

    /// <summary>
    /// Extract product ID from the naming convention "Cached_AR_Product_{id}"
    /// </summary>
    private int ExtractProductId(GameObject obj)
    {
        if (obj == null) return -1;

        string name = obj.name;
        const string prefix = "Cached_AR_Product_";
        if (name.StartsWith(prefix))
        {
            string idStr = name.Substring(prefix.Length);
            if (int.TryParse(idStr, out int id))
            {
                return id;
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns the count of active + pooled objects (for debugging).
    /// </summary>
    public int GetTotalManagedCount()
    {
        return activePlacedObjects.Count + pooledObjects.Count;
    }
}
