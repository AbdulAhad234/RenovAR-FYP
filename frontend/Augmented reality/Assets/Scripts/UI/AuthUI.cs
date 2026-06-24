using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Assuming you are using TextMeshPro for Input Fields

public class AuthUI : MonoBehaviour
{
    [Header("Login UI")]
    public GameObject loginPanel;
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginSubmitButton;
    public Button showSignupButton; // Button to switch to signup panel
    public Button forgotPasswordButton; // Button to bypass login

    [Header("Signup UI")]
    public GameObject signupPanel;
    public TMP_InputField signupNameInput;
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupPasswordInput;
    public Button signupSubmitButton;
    public Button showLoginButton; // Button to switch to login panel

    [Header("Status")]
    public TextMeshProUGUI statusText;

    [Header("Loading UI")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;

    [Header("Navigation")]
    public string nextSceneName = "HomepageScene";

    void Start()
    {
        // Bind Buttons
        if (loginSubmitButton) loginSubmitButton.onClick.AddListener(OnLoginClicked);
        if (signupSubmitButton) signupSubmitButton.onClick.AddListener(OnSignupClicked);
        
        if (showSignupButton) showSignupButton.onClick.AddListener(ShowSignupPanel);
        if (showLoginButton) showLoginButton.onClick.AddListener(ShowLoginPanel);
        if (forgotPasswordButton) forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);

        // Optional: Auto-skip if already logged in
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
        {
            Debug.Log("User is already logged in. Auto-skipping to Homepage.");
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.LoadScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
            return;
        }

        // Initialize panels based on what is available in the current scene
        if (loginPanel != null && signupPanel != null)
        {
            // Both panels present (single-scene setup)
            ShowLoginPanel();
        }
        else if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        else if (signupPanel != null)
        {
            signupPanel.SetActive(true);
        }
    }

    void ShowLoginPanel()
    {
        if (loginPanel) 
        {
            loginPanel.SetActive(true);
            if (signupPanel) signupPanel.SetActive(false);
            SetStatus("");
        }
        else
        {
            Debug.Log("Login panel not found in this scene. Loading LoginScene.");
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.LoadScene("LoginScene");
            }
            else
            {
                SceneManager.LoadScene("LoginScene");
            }
        }
    }

    void ShowSignupPanel()
    {
        if (signupPanel) 
        {
            signupPanel.SetActive(true);
            if (loginPanel) loginPanel.SetActive(false);
            SetStatus("");
        }
        else
        {
            Debug.Log("Signup panel not found in this scene. Loading SignUpScene.");
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.LoadScene("SignUpScene");
            }
            else
            {
                SceneManager.LoadScene("SignUpScene");
            }
        }
    }

    void OnLoginClicked()
    {
        if (AuthManager.Instance == null)
        {
            SetStatus("Error: AuthManager not found in scene!");
            return;
        }

        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please fill in all fields.");
            return;
        }

        SetStatus("Logging in...", true);
        loginSubmitButton.interactable = false;

        AuthManager.Instance.Login(email, password, 
            onSuccess: (msg) => 
            {
                SetStatus("Login successful!", true);
                if (NavigationManager.Instance != null)
                {
                    NavigationManager.Instance.LoadScene(nextSceneName);
                }
                else
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            },
            onError: (err) => 
            {
                SetStatus($"Login failed: {err}", false);
                loginSubmitButton.interactable = true;
            });
    }

    void OnSignupClicked()
    {
        if (AuthManager.Instance == null)
        {
            SetStatus("Error: AuthManager not found in scene!");
            return;
        }

        string name = signupNameInput.text;
        string email = signupEmailInput.text;
        string password = signupPasswordInput.text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please fill in all fields.");
            return;
        }

        SetStatus("Creating account...", true);
        signupSubmitButton.interactable = false;

        AuthManager.Instance.Register(name, email, password, 
            onSuccess: (msg) => 
            {
                SetStatus("Signup successful! Please log in.", false);
                ShowLoginPanel();
                signupSubmitButton.interactable = true;
            },
            onError: (err) => 
            {
                SetStatus($"Signup failed: {err}", false);
                signupSubmitButton.interactable = true;
            });
    }

    void OnForgotPasswordClicked()
    {
        Debug.Log("Bypassing login via Forgot Password button.");
        PlayerPrefs.SetString("AuthToken", "bypass_mock_token");
        PlayerPrefs.Save();
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void SetStatus(string msg, bool showLoadingPanel = false)
    {
        // Update both texts
        if (statusText) statusText.text = msg;
        if (loadingStatusText) loadingStatusText.text = msg;

        // Toggle the loading panel visibility
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(showLoadingPanel);
        }

        Debug.Log(msg);
    }
}
