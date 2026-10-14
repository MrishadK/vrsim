using UnityEngine;
using UnityEditor;

public static class FixBoeingLivery
{
    [MenuItem("Tools/Apply Boeing Livery")]
    public static void ExecuteFix()
    {
        SetupPhotorealisticBoeing737.ExecuteUpgrade();
    }
}

