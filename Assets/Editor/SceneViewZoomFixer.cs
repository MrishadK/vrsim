using UnityEditor;
using UnityEngine;

/// <summary>
/// AeroSim VR — Scene View & Camera Zoom / Near-Clip Fixer.
/// Prevents the floor and foreground geometry from disappearing / clipping
/// when zooming into terminal interiors or tarmac details.
///
/// In large scenes (like airport aprons), Unity's default 'Dynamic Clipping'
/// inflates the Scene View near clip plane to several meters, causing the floor
/// to slice off and zoom to crawl. This tool sets near clip to 0.02m (2 cm)
/// and disables dynamic clipping for smooth, full-detail close-up navigation.
/// </summary>
[InitializeOnLoad]
public static class SceneViewZoomFixer
{
    static SceneViewZoomFixer()
    {
        EditorApplication.delayCall += ApplyFixToAllSceneViews;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (sceneView == null) return;

        // Ensure dynamic clipping is off and near clip is razor-sharp (2cm)
        if (sceneView.cameraSettings != null)
        {
            if (sceneView.cameraSettings.dynamicClipping || sceneView.cameraSettings.nearClip > 0.05f)
            {
                sceneView.cameraSettings.dynamicClipping = false;
                sceneView.cameraSettings.nearClip = 0.02f;
                if (sceneView.camera != null)
                {
                    sceneView.camera.nearClipPlane = 0.02f;
                }
            }
        }
    }

    [MenuItem("Tools/Aviation/Fix Scene View Zoom & Floor Clipping %&f")] // Ctrl+Alt+F (or Cmd+Alt+F)
    public static void ApplyFixToAllSceneViews()
    {
        int count = 0;
        foreach (SceneView sv in SceneView.sceneViews)
        {
            if (sv != null && sv.cameraSettings != null)
            {
                sv.cameraSettings.dynamicClipping = false;
                sv.cameraSettings.nearClip = 0.02f;
                if (sv.camera != null)
                {
                    sv.camera.nearClipPlane = 0.02f;
                }
                sv.Repaint();
                count++;
            }
        }

        // Also fix Main Camera if in the scene
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindObjectOfType<Camera>();
        if (mainCam != null && mainCam.nearClipPlane > 0.05f)
        {
            Undo.RecordObject(mainCam, "Set Camera Near Clip");
            mainCam.nearClipPlane = 0.02f;
            EditorUtility.SetDirty(mainCam);
        }

        Debug.Log($"<color=#00e676><b>[SceneViewZoomFixer]</b></color> Configured {count} SceneView(s) with nearClip=0.02m and Dynamic Clipping disabled. Floor will no longer disappear when zooming!");
    }

    [MenuItem("Tools/Aviation/Focus View on Terminal Lounge & Floor")]
    public static void FocusLounge()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null && SceneView.sceneViews.Count > 0)
        {
            sv = (SceneView)SceneView.sceneViews[0];
        }

        if (sv != null)
        {
            sv.cameraSettings.dynamicClipping = false;
            sv.cameraSettings.nearClip = 0.02f;
            sv.pivot = new Vector3(20f, 2.5f, 30f); // Terminal lounge center
            sv.size = 5f; // Close distance for smooth responsive zoom
            sv.rotation = Quaternion.Euler(6f, -35f, 0f);
            if (sv.camera != null) sv.camera.nearClipPlane = 0.02f;
            sv.Repaint();
            Debug.Log("<color=#00e676><b>[SceneViewZoomFixer]</b></color> Focused Scene View onto Terminal Lounge with smooth zoom pivot.");
        }
    }
}
