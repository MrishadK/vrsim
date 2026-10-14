using UnityEngine;
using UnityEditor;

public static class FocusMainCamera
{

    [MenuItem("Tools/Focus Main Camera #&c", false, 1)]
    public static void FocusOnMainCamera()
    {
        GameObject camObj = FindMainCamera();

        if (camObj == null)
        {
            Debug.LogWarning("[FocusMainCamera] No Main Camera found in the current scene.");
            return;
        }

        // Select the camera GameObject
        Selection.activeGameObject = camObj;
        EditorGUIUtility.PingObject(camObj);

        // Frame the camera in the Scene View
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.Focus();
        }
        else if (SceneView.sceneViews.Count > 0)
        {
            SceneView sv = (SceneView)SceneView.sceneViews[0];
            sv.Frame(new Bounds(camObj.transform.position, Vector3.one * 2f), false);
            sv.Focus();
        }

        Debug.Log($"[FocusMainCamera] Focused and selected '{camObj.name}' at position {camObj.transform.position}");
    }

    [MenuItem("Tools/Align Scene View to Main Camera", false, 2)]
    public static void AlignSceneViewToCamera()
    {
        GameObject camObj = FindMainCamera();
        if (camObj == null)
        {
            Debug.LogWarning("[FocusMainCamera] No Main Camera found in the current scene.");
            return;
        }

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.AlignViewToObject(camObj.transform);
            SceneView.lastActiveSceneView.Focus();
            Debug.Log($"[FocusMainCamera] Aligned Scene View to '{camObj.name}' view.");
        }
    }

    public static GameObject FindMainCamera()
    {
        // 1. Try Camera.main
        if (Camera.main != null)
        {
            return Camera.main.gameObject;
        }

        // 2. Search by tag
        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("MainCamera");
            if (tagged != null) return tagged;
        }
        catch { }

        // 3. Search by name
        GameObject byName = GameObject.Find("Main Camera");
        if (byName != null) return byName;

        // 4. Search any Camera in scene
        Camera anyCam = Object.FindAnyObjectByType<Camera>();
        if (anyCam != null) return anyCam.gameObject;

        return null;
    }
}
