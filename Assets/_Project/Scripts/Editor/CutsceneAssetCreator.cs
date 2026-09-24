using UnityEngine;
using UnityEditor;
using MainKarinding.Cutscenes;

namespace MainKarinding.Editor
{
    public static class CutsceneAssetCreator
    {
        [MenuItem("Main Karinding/Cutscenes/Create Scene1 Sprite Sets")]
        public static void CreateScene1SpriteSets()
        {
            // Create RimbaSpriteSet
            var rimbaSet = ScriptableObject.CreateInstance<RimbaSpriteSet>();
            AssetDatabase.CreateAsset(rimbaSet, "Assets/_Project/Data/Cutscenes/Chapter1/Scene1/RimbaSpriteSet.asset");

            // Create AbahSpriteSet
            var abahSet = ScriptableObject.CreateInstance<AbahSpriteSet>();
            AssetDatabase.CreateAsset(abahSet, "Assets/_Project/Data/Cutscenes/Chapter1/Scene1/AbahSpriteSet.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CutsceneAssetCreator] Scene1 sprite sets created successfully!");
        }
    }
}
