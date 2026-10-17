using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AeroSim VR — Editor utility to switch to the Game Screen (SampleScene.unity).
/// Can be invoked via menu: Tools -> Aviation -> Switch to Game Screen (Ctrl+Alt+G)
/// </summary>
[InitializeOnLoad]
public static class OpenGameScreen
{
    public const string GAME_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    public const string FLAG_PATH = "Temp/OpenGameScreen.flag";

    static OpenGameScreen()
    {
        EditorApplication.delayCall += CheckAutoOpen;
    }

    private static void CheckAutoOpen()
    {
        if (File.Exists(FLAG_PATH))
        {
            try { File.Delete(FLAG_PATH); } catch { }
            Execute();
        }
    }

    [MenuItem("Tools/Aviation/Switch to Game Screen (SampleScene) %&g", false, 1)]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != GAME_SCENE_PATH)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log($"<color=#00FFCC>[GameScreen]</color> Loading Game Scene: {GAME_SCENE_PATH}");
                EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
                Debug.Log($"<color=#00FFCC>[GameScreen]</color> Successfully moved to Game Screen ({GAME_SCENE_PATH})!");
            }
        }
        else
        {
            Debug.Log($"<color=#00FFCC>[GameScreen]</color> Already on Game Screen ({GAME_SCENE_PATH}).");
        }
    }
}
