using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CastleBusters
{
    public static class InspectSceneBlocks
    {
        [MenuItem("CastleBusters/Inspect Scene Blocks")]
        public static void Inspect()
        {            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var blocks = Object.FindObjectsOfType<DestructibleBlock>();
            Debug.Log($"Found {blocks.Length} DestructibleBlocks in the scene.");
            foreach (var block in blocks)
            {
                Debug.Log($"Block: {block.name}, Position: {block.transform.position}, BlockData: {(block.blockData != null ? block.blockData.name : "null")}");
            }
        }
    }
}
