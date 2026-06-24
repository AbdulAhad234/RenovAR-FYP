using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class CartItem
{
    public ProductData product;
    public bool isSelected = true;

    public CartItem(ProductData product)
    {
        this.product = product;
        this.isSelected = true;
    }
}

[Serializable]
public class CartData
{
    public List<CartItem> items = new List<CartItem>();
}

public class CartManager : MonoBehaviour
{
    private static CartManager _instance;
    public static CartManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CartManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CartManager (Auto)");
                    _instance = go.AddComponent<CartManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private const string CART_KEY = "UserCartData";
    private List<CartItem> cartItems = new List<CartItem>();

    public event Action OnCartUpdated;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCart();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public List<CartItem> GetItems() => cartItems;
    public int GetCartCount() => cartItems.Count;

    public void AddToCart(ProductData product)
    {
        // Check if already in cart
        if (cartItems.Exists(i => i.product.id == product.id))
        {
            Debug.Log($"{product.name} is already in the cart.");
            return;
        }

        cartItems.Add(new CartItem(product));
        SaveCart();
        OnCartUpdated?.Invoke();
    }

    public void RemoveFromCart(int productId)
    {
        cartItems.RemoveAll(i => i.product.id == productId);
        SaveCart();
        OnCartUpdated?.Invoke();
    }

    public void ToggleSelection(int productId, bool selected)
    {
        var item = cartItems.Find(i => i.product.id == productId);
        if (item != null)
        {
            item.isSelected = selected;
            SaveCart();
            OnCartUpdated?.Invoke();
        }
    }

    public float CalculateTotal()
    {
        float total = 0;
        foreach (var item in cartItems)
        {
            if (item.isSelected)
            {
                if (float.TryParse(item.product.price, out float price))
                {
                    total += price;
                }
            }
        }
        return total;
    }

    public float CalculateSubtotal()
    {
        float total = 0;
        foreach (var item in cartItems)
        {
            if (float.TryParse(item.product.price, out float price))
            {
                total += price;
            }
        }
        return total;
    }

    private void SaveCart()
    {
        CartData data = new CartData { items = cartItems };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(CART_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadCart()
    {
        if (PlayerPrefs.HasKey(CART_KEY))
        {
            string json = PlayerPrefs.GetString(CART_KEY);
            try
            {
                CartData data = JsonUtility.FromJson<CartData>(json);
                cartItems = data.items ?? new List<CartItem>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load cart: {e.Message}");
                cartItems = new List<CartItem>();
            }
        }
    }
}
