using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CartItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image productImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Toggle selectionToggle;
    public Button deleteButton;

    private CartItem cartItem;

    public void Setup(CartItem item, Action<int, bool> onToggle, Action<int> onDelete)
    {
        cartItem = item;

        if (nameText != null) nameText.text = item.product.name;
        
        if (priceText != null)
        {
            string symbol = string.IsNullOrEmpty(item.product.currencySymbol) ? "PKR" : item.product.currencySymbol;
            priceText.text = $"{symbol} {item.product.price}";
        }

        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.RemoveAllListeners();
            selectionToggle.isOn = item.isSelected;
            selectionToggle.onValueChanged.AddListener((val) => onToggle?.Invoke(item.product.id, val));
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(item.product.id));
        }

        // Load image
        if (ImageCache.HasCachedImage(item.product.id, item.product.thumbnailUrl))
        {
            productImage.sprite = ImageCache.LoadCachedImage(item.product.id, item.product.thumbnailUrl);
        }
        else if (!string.IsNullOrEmpty(item.product.thumbnailUrl))
        {
            int targetProductId = item.product.id;
            StartCoroutine(ImageCache.DownloadAndCache(item.product.thumbnailUrl, item.product.id, (sprite) =>
            {
                if (cartItem != null && cartItem.product.id == targetProductId && sprite != null && productImage != null)
                {
                    productImage.sprite = sprite;
                }
            }));
        }
    }
}
