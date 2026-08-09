using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CastleBusters
{
    public static class InspectSceneHierarchy
    {
        [MenuItem("CastleBusters/Inspect Scene Hierarchy")]
        public static void Inspect()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                PrintHierarchy(root, "");
            }
        }

        private static void PrintHierarchy(GameObject go, string indent)
        {
            Debug.Log($"{indent}{go.name} (Active: {go.activeSelf})");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                PrintHierarchy(go.transform.GetChild(i).gameObject, indent + "  ");
            }
        }
    }
}
