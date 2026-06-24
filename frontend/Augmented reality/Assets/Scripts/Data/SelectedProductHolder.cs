using UnityEngine;

public class SelectedProductHolder : MonoBehaviour
{
    public static SelectedProductHolder Instance;

    private GameObject selectedPrefab;
    private ProductData selectedProductData;
    private ARProductData selectedARData;
    private Texture2D selectedTexture;
    [System.NonSerialized] public bool cameFromHomepage = false;
    [System.NonSerialized] public bool isTextureMode = false;

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

    public void SetSelectedProduct(GameObject prefab)
    {
        selectedPrefab = prefab;
    }

    public GameObject GetSelectedProduct()
    {
        return selectedPrefab;
    }

    public void SetSelectedProductData(ProductData data)
    {
        selectedProductData = data;
        ClearTexture();
    }

    public ProductData GetSelectedProductData()
    {
        return selectedProductData;
    }

    public void SetSelectedARData(ARProductData data)
    {
        selectedARData = data;
    }

    public ARProductData GetSelectedARData()
    {
        return selectedARData;
    }

    public void SetSelectedTexture(Texture2D texture)
    {
        selectedTexture = texture;
    }

    public Texture2D GetSelectedTexture()
    {
        return selectedTexture;
    }

    public void ClearTexture()
    {
        selectedTexture = null;
        isTextureMode = false;
    }
}
