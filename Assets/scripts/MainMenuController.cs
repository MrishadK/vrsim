using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// AeroSim VR — Main Menu Controller.
/// Handles smooth asynchronous scene loading with a professional loading animation.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the game scene to load.")]
    public string gameSceneName = "SampleScene";

    [Header("Loading UI References")]
    public GameObject loadingPanel;
    public Image progressBarFill;
    public TextMeshProUGUI loadingText;
    public Transform spinnerIcon;

    private bool isLoading;

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;
        Time.timeScale = 1f;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            StartCoroutine(LoadSceneAsyncRoutine());
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private IEnumerator LoadSceneAsyncRoutine()
    {
        Debug.Log("[AeroSim VR] Starting async load for: " + gameSceneName);
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(gameSceneName);
        asyncOp.allowSceneActivation = false; // Hold scene activation until transition completes

        float displayProgress = 0f;

        while (displayProgress < 0.99f)
        {
            // Unity asyncOp.progress goes 0.0 -> 0.9 while loading, then 0.9 -> 1.0 on activation
            float rawProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, rawProgress, Time.deltaTime * 1.2f);

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = displayProgress;
            }

            if (loadingText != null)
            {
                int percent = Mathf.RoundToInt(displayProgress * 100f);
                loadingText.text = $"PREPARING TERMINAL... {percent}%";
            }

            if (spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -250f * Time.deltaTime);
            }

            yield return null;
        }

        // Final completion state
        if (progressBarFill != null) progressBarFill.fillAmount = 1f;
        if (loadingText != null) loadingText.text = "SIMULATION READY! ENTERING...";

        yield return new WaitForSeconds(0.4f);

        asyncOp.allowSceneActivation = true;
    }

    public void QuitGame()
    {
        Debug.Log("[AeroSim VR] Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
