using UnityEditor;
using UnityEngine;

public static class BuildSettingsSetup
{
    public static void AddSampleScene()
    {
        var original = EditorBuildSettings.scenes;
        var newScenes = new EditorBuildSettingsScene[original.Length + 1];
        System.Array.Copy(original, newScenes, original.Length);
        newScenes[original.Length] = new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("Added SampleScene to Build Settings");
    }
}