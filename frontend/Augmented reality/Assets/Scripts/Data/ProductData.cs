using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ProductDimensions
{
    public string unit;
    public float depth;
    public float width;
    public float height;
}

[System.Serializable]
public class ProductData
{
    public int id;
    public string name;
    public string description;
    public string price;
    public int categoryId;
    public string categoryName;
    public string thumbnailUrl;
    public int modelsCount;
    public int texturesCount;
    public string currency;
    public string currencySymbol;
    public string brandName;
    public string vendorName;
    public ProductDimensions dimensions;

    /// <summary>
    /// Returns true if this product is a 3D model (furniture, decor, etc.)
    /// </summary>
    public bool Is3DModel => modelsCount > 0;

    /// <summary>
    /// Returns true if this product is a texture/material (flooring, wall paneling, etc.)
    /// </summary>
    public bool IsTexture
    {
        get
        {
            if (texturesCount > 0 && modelsCount == 0) return true;
            string cat = (categoryName ?? "").ToLower();
            return cat.Contains("texture") || cat.Contains("flooring") || cat.Contains("paneling")
                || cat.Contains("marble") || cat.Contains("tile") || cat.Contains("wallpaper");
        }
    }

    /// <summary>
    /// Returns true if this texture should be applied to floor planes.
    /// </summary>
    public bool IsFloorTexture
    {
        get
        {
            string cat = (categoryName ?? "").ToLower();
            string n = (name ?? "").ToLower();
            return cat.Contains("floor") || cat.Contains("marble") || cat.Contains("tile")
                || n.Contains("floor") || n.Contains("marble") || n.Contains("tile")
                || n.Contains("carpet") || n.Contains("rug") || n.Contains("wood floor")
                || n.Contains("vinyl") || n.Contains("laminate") || n.Contains("parquet");
        }
    }

    /// <summary>
    /// Returns true if this texture should be applied to wall planes.
    /// </summary>
    public bool IsWallTexture
    {
        get
        {
            string cat = (categoryName ?? "").ToLower();
            string n = (name ?? "").ToLower();
            return cat.Contains("wall") || cat.Contains("paneling") || cat.Contains("wallpaper")
                || n.Contains("wall") || n.Contains("panel") || n.Contains("wallpaper")
                || n.Contains("brick") || n.Contains("stone wall") || n.Contains("backsplash");
        }
    }
}

[System.Serializable]
public class PaginationData
{
    public int page;
    public int limit;
    public int total;
    public int totalPages;
    public bool hasNext;
    public bool hasPrev;
}

[System.Serializable]
public class MarketplaceData
{
    public ProductData[] data;
    public PaginationData pagination;
}

[System.Serializable]
public class MarketplaceResponse
{
    public bool success;
    public string message;
    public MarketplaceData data;
}

[System.Serializable]
public class ARPlacementData
{
    public string anchorType;
    public List<string> allowedPlaneClassifications;
    public string fallbackPlaneClassification;
}

[System.Serializable]
public class ARProductData
{
    public int id;
    public string name;
    public string price;
    public string modelUrl;
    public string thumbnailUrl;
    public ARPlacementData arPlacement;
}

[System.Serializable]
public class ARProductResponse
{
    public bool success;
    public string message;
    public ARProductData data;
}
