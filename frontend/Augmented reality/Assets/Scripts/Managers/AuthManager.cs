using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if USE_FIREBASE
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
#endif

[System.Serializable]
public class AuthRequest
{
    public string email;
    public string password;
    public string name; // Used for registration
}

[System.Serializable]
public class SyncRequest
{
    public string role = "customer";
}

[System.Serializable]
public class SyncResponseData
{
    public int id;
    public string firebaseUid;
    public string email;
    public string role;
}

[System.Serializable]
public class AuthResponse
{
    public bool success;
    public string message;
    public SyncResponseData data;
}

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    [Tooltip("e.g. https://renovar-backend-8fg8.onrender.com/api")]
    public string backendBaseUrl = "https://renovar-backend-8fg8.onrender.com/api";

    [Tooltip("If true and USE_FIREBASE is not defined, runs in local mock bypass mode.")]
    public bool useMockAuth = true;

    private const string TOKEN_KEY = "AuthToken";

#if USE_FIREBASE
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private bool isFirebaseInitialized = false;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
#if USE_FIREBASE
            InitializeFirebase();
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if USE_FIREBASE
    private void InitializeFirebase()
    {
        try
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Firebase initialization task failed or canceled: {task.Exception?.GetBaseException()?.Message}");
                    return;
                }
                
                try
                {
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == DependencyStatus.Available) {
                        auth = FirebaseAuth.DefaultInstance;
                        isFirebaseInitialized = true;
                        Debug.Log("Firebase Auth initialized successfully.");
                    } else {
                        Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception inside Firebase initialization task continuation: {ex.Message}");
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase initialization exception: {e.Message}");
        }
    }
#endif

    public void Login(string email, string password, Action<string> onSuccess, Action<string> onError)
    {
#if USE_FIREBASE
        if (isFirebaseInitialized)
        {
            StartCoroutine(FirebaseLoginRoutine(email, password, onSuccess, onError));
            return;
        }
#endif

        if (useMockAuth)
        {
            Debug.LogWarning("AuthManager: Firebase Auth is not initialized. Falling back to local Mock Auth.");
            StartCoroutine(SimulateMockLogin(onSuccess));
        }
        else
        {
            onError?.Invoke("Firebase SDK not initialized or compiled. Please define USE_FIREBASE symbol.");
        }
    }

    public void Register(string name, string email, string password, Action<string> onSuccess, Action<string> onError)
    {
#if USE_FIREBASE
        if (isFirebaseInitialized)
        {
            StartCoroutine(FirebaseRegisterRoutine(email, password, onSuccess, onError));
            return;
        }
#endif

        if (useMockAuth)
        {
            Debug.LogWarning("AuthManager: Firebase Auth is not initialized. Falling back to local Mock Auth.");
            StartCoroutine(SimulateMockRegister(onSuccess));
        }
        else
        {
            onError?.Invoke("Firebase SDK not initialized or compiled. Please define USE_FIREBASE symbol.");
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.Save();
#if USE_FIREBASE
        if (auth != null)
        {
            auth.SignOut();
        }
#endif
    }

    public bool IsLoggedIn()
    {
        return PlayerPrefs.HasKey(TOKEN_KEY) && !string.IsNullOrEmpty(PlayerPrefs.GetString(TOKEN_KEY));
    }

    public string GetToken()
    {
        return PlayerPrefs.GetString(TOKEN_KEY, "");
    }

    private IEnumerator SimulateMockLogin(Action<string> onSuccess)
    {
        yield return new WaitForSeconds(0.8f);
        PlayerPrefs.SetString(TOKEN_KEY, "mock_token_for_development");
        PlayerPrefs.Save();
        onSuccess?.Invoke("Success!");
    }

    private IEnumerator SimulateMockRegister(Action<string> onSuccess)
    {
        yield return new WaitForSeconds(0.8f);
        onSuccess?.Invoke("Success!");
    }

#if USE_FIREBASE
    private IEnumerator FirebaseLoginRoutine(string email, string password, Action<string> onSuccess, Action<string> onError)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.IsFaulted || loginTask.IsCanceled)
        {
            Exception ex = loginTask.Exception?.GetBaseException() ?? new Exception("Firebase login canceled");
            Debug.LogError($"Firebase Login Error: {ex.Message}");
            onError?.Invoke(ex.Message);
        }
        else
        {
            AuthResult result = loginTask.Result;
            currentUser = result.User;
            Debug.Log($"Firebase Login Succeeded for user: {currentUser.Email}");

            // Retrieve the ID Token to send to the backend
            var tokenTask = currentUser.TokenAsync(true);
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            if (tokenTask.IsFaulted || tokenTask.IsCanceled)
            {
                onError?.Invoke("Failed to retrieve Firebase ID token.");
            }
            else
            {
                string idToken = tokenTask.Result;
                yield return StartCoroutine(SyncUserWithBackend(idToken, onSuccess, onError));
            }
        }
    }

    private IEnumerator FirebaseRegisterRoutine(string email, string password, Action<string> onSuccess, Action<string> onError)
    {
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsFaulted || registerTask.IsCanceled)
        {
            Exception ex = registerTask.Exception?.GetBaseException() ?? new Exception("Firebase signup canceled");
            Debug.LogError($"Firebase Signup Error: {ex.Message}");
            onError?.Invoke(ex.Message);
        }
        else
        {
            AuthResult result = registerTask.Result;
            currentUser = result.User;
            Debug.Log($"Firebase Signup Succeeded for user: {currentUser.Email}");

            // Retrieve the ID Token to send to the backend
            var tokenTask = currentUser.TokenAsync(true);
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            if (tokenTask.IsFaulted || tokenTask.IsCanceled)
            {
                onError?.Invoke("Failed to retrieve Firebase ID token after registration.");
            }
            else
            {
                string idToken = tokenTask.Result;
                yield return StartCoroutine(SyncUserWithBackend(idToken, onSuccess, onError));
            }
        }
    }

    private IEnumerator SyncUserWithBackend(string idToken, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{backendBaseUrl}/auth/sync";
        SyncRequest syncData = new SyncRequest();
        string jsonRequest = JsonUtility.ToJson(syncData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {idToken}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Sync Error ({webRequest.responseCode}): {webRequest.error}\n{webRequest.downloadHandler.text}");
                onError?.Invoke($"Sync failed: {webRequest.downloadHandler.text}");
            }
            else
            {
                try
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(jsonResponse);

                    if (response != null && response.success)
                    {
                        // Save the Firebase ID token in PlayerPrefs for other API calls to use
                        PlayerPrefs.SetString(TOKEN_KEY, idToken);
                        PlayerPrefs.Save();
                        
                        onSuccess?.Invoke("User synchronized successfully!");
                    }
                    else
                    {
                        Debug.LogWarning("Sync response parsed but success flag is false: " + jsonResponse);
                        onError?.Invoke("Sync failed: unexpected response status.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing sync response: {e.Message}");
                    onError?.Invoke("Failed to parse synchronization response.");
                }
            }
        }
    }
#endif
}
