#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CastleBusters.Editor
{
    /// <summary>
    /// Applies the release-safe orientation baseline before building Android or iOS players.
    /// Store identities and signing credentials deliberately remain project-owner configuration.
    /// </summary>
    public static class MobileReleaseBuildSettings
    {

        [MenuItem("Castle Busters/Apply Mobile Landscape Release Baseline")]
        public static void Apply()
        {
            // The battlefield, panels, and CanvasScaler reference size are all landscape.
            // Mobile players may rotate either direction; notches are handled at runtime.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            AssetDatabase.SaveAssets();
            Debug.Log("Applied mobile release baseline: landscape auto-rotation and IL2CPP for Android and iOS.");
        }
    }
}
#endif
