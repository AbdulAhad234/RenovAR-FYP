using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.XR.ARFoundation; // Added for LoaderUtility

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance { get; private set; }

    // Stack for scenes
    private Stack<string> sceneHistory = new Stack<string>();
    private string currentScene;
    private bool isGoingBack = false;

    // Stack for local UI panels/actions within the current scene
    private Stack<Action> localBackActions = new Stack<Action>();
    private bool arSubsystemsInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("NavigationManager");
            Instance = go.AddComponent<NavigationManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Force target frame rate to 60 FPS on mobile for butter-smooth UI & interactions
        Application.targetFrameRate = 60;
        
        try
        {
            currentScene = SceneManager.GetActiveScene().name;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"NavigationManager: Could not get active scene name during Awake: {e.Message}");
            currentScene = "";
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private bool IsARScene(string sceneName)
    {
        return sceneName == "ProductModelScene" || sceneName == "ProductTextureScene";
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            // If we transitioned to a non-AR scene, clean up AR loader subsystems to free destroyed camera references
            if (!IsARScene(scene.name))
            {
                if (arSubsystemsInitialized)
                {
                    try
                    {
                        LoaderUtility.Deinitialize();
                        arSubsystemsInitialized = false;
                        Debug.Log($"NavigationManager: Deinitialized AR Loader after loading non-AR scene: {scene.name}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"NavigationManager: Safe catch during LoaderUtility.Deinitialize: {e.Message}");
                    }
                }
            }

            // Clear local panel history when a new scene loads completely
            localBackActions.Clear();

            if (isGoingBack)
            {
                isGoingBack = false;
                currentScene = scene.name;
            }
            else
            {
                if (scene.name != currentScene)
                {
                    sceneHistory.Push(currentScene);
                    currentScene = scene.name;
                }
            }
        }
    }

    /// <summary>
    /// Registers a local panel close action. Call this when opening a panel dynamically.
    /// </summary>
    public void PushLocalAction(Action closeAction)
    {
        localBackActions.Push(closeAction);
    }

    /// <summary>
    /// Removes a local panel action from the stack without executing it. 
    /// Call this if a panel is closed manually without using the back button.
    /// </summary>
    public void PopLocalAction()
    {
        if (localBackActions.Count > 0)
        {
            localBackActions.Pop();
        }
    }

    /// <summary>
    /// Forcefully clears all local panel actions. Call this when returning to a root UI panel to prevent ghost actions.
    /// </summary>
    public void ClearLocalActions()
    {
        localBackActions.Clear();
    }

    /// <summary>
    /// Safely removes a specific local panel action from the stack if present.
    /// </summary>
    public void RemoveLocalAction(Action actionToRemove)
    {
        if (localBackActions == null || localBackActions.Count == 0 || actionToRemove == null)
            return;

        List<Action> temp = new List<Action>();
        while (localBackActions.Count > 0)
        {
            Action act = localBackActions.Pop();
            if (act != actionToRemove)
            {
                temp.Add(act);
            }
        }
        
        // Push remaining actions back in reverse order
        for (int i = temp.Count - 1; i >= 0; i--)
        {
            localBackActions.Push(temp[i]);
        }
    }

    public void GoBack()
    {
        Debug.Log($"[NavigationManager] GoBack called. Local actions: {localBackActions.Count}, Scene history: {sceneHistory.Count}");

        // 1. Try to close the top-most local UI panel first
        if (localBackActions.Count > 0)
        {
            Action closeAction = localBackActions.Pop();
            Debug.Log($"[NavigationManager] Executing local back action: {closeAction?.Method.Name}");
            closeAction?.Invoke();
            return;
        }

        // 2. If no local panels are open, go back to the previous scene
        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory.Pop();
            Debug.Log($"[NavigationManager] Returning to previous scene: {previousScene}");
            isGoingBack = true;
            LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("[NavigationManager] No history available. Defaulting to HomepageScene.");
            isGoingBack = true;
            LoadScene("HomepageScene");
        }
    }

    public void ClearHistory()
    {
        sceneHistory.Clear();
        localBackActions.Clear();
        currentScene = SceneManager.GetActiveScene().name;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        // Initialize AR loader BEFORE loading the AR scene to prevent iOS crashes
        if (IsARScene(sceneName))
        {
            try
            {
                LoaderUtility.Initialize();
                arSubsystemsInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"NavigationManager: Failed to initialize AR Loader: {e.Message}");
            }
            
            // Give the subsystem 1 frame to spin up before switching scenes
            yield return null;
        }

        // Just load the scene directly without any custom loading UI
        SceneManager.LoadScene(sceneName);
    }
}
