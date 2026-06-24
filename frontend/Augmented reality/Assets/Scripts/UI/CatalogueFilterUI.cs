using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class CatalogueFilterUI : MonoBehaviour
{
    [Header("Filter Buttons")]
    public Button brandButton;
    public Button typeButton;
    public Button vendorButton;
    public Button priceButton;

    [Header("Popup UI")]
    public GameObject filterPopupPanel;
    public TextMeshProUGUI filterPopupText;
    public Button closePopupButton;

    [Header("References")]
    public ProductGridSpawner gridSpawner;

    // Filter States
    private bool isBrandOn = false;
    private string activeBrand = "";

    private bool isTypeOn = false;
    private string activeType = "";

    private bool isVendorOn = false;
    private string activeVendor = "";

    private int priceState = 0; // 0 = Off, 1 = Highest->Lowest, 2 = Lowest->Highest

    void Start()
    {
        if (brandButton) brandButton.onClick.AddListener(ToggleBrand);
        if (typeButton) typeButton.onClick.AddListener(ToggleType);
        if (vendorButton) vendorButton.onClick.AddListener(ToggleVendor);
        if (priceButton) priceButton.onClick.AddListener(TogglePrice);
        if (closePopupButton) closePopupButton.onClick.AddListener(ClosePopup);

        if (filterPopupPanel) filterPopupPanel.SetActive(false);
    }

    void OnDisable()
    {
        // Auto-close popup when the Catalogue panel is hidden
        ClosePopup();
    }

    private void ToggleBrand()
    {
        isBrandOn = !isBrandOn;
        if (isBrandOn)
        {
            // Lock in the top brand based on CURRENT active filters
            var currentList = GetFilteredList(ignoreBrand: true);
            var topGroup = currentList.Where(p => !string.IsNullOrEmpty(p.brandName))
                                      .GroupBy(p => p.brandName)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault();
            activeBrand = topGroup != null ? topGroup.Key : "";
        }
        else
        {
            activeBrand = "";
        }
        ApplyFiltersAndShowPopup("Brand");
    }

    private void ToggleType()
    {
        isTypeOn = !isTypeOn;
        if (isTypeOn)
        {
            // Lock in the top type based on CURRENT active filters
            var currentList = GetFilteredList(ignoreType: true);
            var topGroup = currentList.Where(p => !string.IsNullOrEmpty(p.categoryName))
                                      .GroupBy(p => p.categoryName)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault();
            activeType = topGroup != null ? topGroup.Key : "";
        }
        else
        {
            activeType = "";
        }
        ApplyFiltersAndShowPopup("Type");
    }

    private void ToggleVendor()
    {
        isVendorOn = !isVendorOn;
        if (isVendorOn)
        {
            // Lock in the top vendor based on CURRENT active filters
            var currentList = GetFilteredList(ignoreVendor: true);
            var topGroup = currentList.Where(p => !string.IsNullOrEmpty(p.vendorName))
                                      .GroupBy(p => p.vendorName)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault();
            activeVendor = topGroup != null ? topGroup.Key : "";
        }
        else
        {
            activeVendor = "";
        }
        ApplyFiltersAndShowPopup("Vendor");
    }

    private void TogglePrice()
    {
        priceState = (priceState + 1) % 3;
        ApplyFiltersAndShowPopup("Price");
    }

    private void ClosePopup()
    {
        if (filterPopupPanel) filterPopupPanel.SetActive(false);
    }

    /// <summary>
    /// Evaluates the master product list against all active filters, optionally ignoring one.
    /// This allows us to calculate the "Top Brand" within the already filtered results (e.g. Top Brand of Chairs).
    /// </summary>
    private IEnumerable<ProductData> GetFilteredList(bool ignoreBrand = false, bool ignoreType = false, bool ignoreVendor = false)
    {
        if (gridSpawner == null) return new List<ProductData>();
        
        ProductData[] baseProducts = gridSpawner.GetBaseProducts();
        if (baseProducts == null || baseProducts.Length == 0) return new List<ProductData>();

        IEnumerable<ProductData> filtered = baseProducts;

        // Apply filters cumulatively
        if (isBrandOn && !ignoreBrand && !string.IsNullOrEmpty(activeBrand))
        {
            filtered = filtered.Where(p => p.brandName == activeBrand);
        }
        if (isTypeOn && !ignoreType && !string.IsNullOrEmpty(activeType))
        {
            filtered = filtered.Where(p => p.categoryName == activeType);
        }
        if (isVendorOn && !ignoreVendor && !string.IsNullOrEmpty(activeVendor))
        {
            filtered = filtered.Where(p => p.vendorName == activeVendor);
        }

        return filtered;
    }

    public void ApplyFiltersAndShowPopup(string changedFilter)
    {
        if (gridSpawner == null) return;

        // 1. Get the fully filtered base list applying all active toggles
        IEnumerable<ProductData> filtered = GetFilteredList();
        int count = filtered.Count();

        // 2. Build a dynamic popup message describing the exact combination
        List<string> activeNames = new List<string>();
        if (isBrandOn && !string.IsNullOrEmpty(activeBrand)) activeNames.Add(activeBrand);
        if (isTypeOn && !string.IsNullOrEmpty(activeType)) activeNames.Add(activeType);
        if (isVendorOn && !string.IsNullOrEmpty(activeVendor)) activeNames.Add($"from {activeVendor}");

        string filterDesc = activeNames.Count > 0 ? string.Join(" ", activeNames) : "all";
        string popupMessage = $"We have {count} {filterDesc} products, showing them now.";

        // 3. Apply secondary sorting (Price) LAST to the finalized result set
        if (priceState == 1)
        {
            filtered = filtered.OrderByDescending(p => ParsePrice(p.price));
            if (changedFilter == "Price")
                popupMessage = $"Showing {count} {filterDesc} products from Highest to Lowest price.";
        }
        else if (priceState == 2)
        {
            filtered = filtered.OrderBy(p => ParsePrice(p.price));
            if (changedFilter == "Price")
                popupMessage = $"Showing {count} {filterDesc} products from Lowest to Highest price.";
        }

        // 4. Render the newly filtered and sorted array immediately
        ProductData[] finalArray = filtered.ToArray();
        gridSpawner.RenderProducts(finalArray);

        // 5. Update the popup UI if any filter is active
        if (isBrandOn || isTypeOn || isVendorOn || priceState != 0)
        {
            if (filterPopupText) filterPopupText.text = popupMessage;
            if (filterPopupPanel) filterPopupPanel.SetActive(true);
        }
        else
        {
            // All filters were turned off
            ClosePopup();
        }
    }

    private float ParsePrice(string priceStr)
    {
        if (string.IsNullOrEmpty(priceStr)) return 0f;
        
        string cleanStr = new string(priceStr.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (float.TryParse(cleanStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0f;
    }
}
