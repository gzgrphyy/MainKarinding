using UnityEngine;
using UnityEditor;
using System.IO;

namespace MainKarinding.Editor
{
    public static class TextureImportHelper
    {
        [MenuItem("Main Karinding/Setup/Configure All Character Sprites")]
        public static void ConfigureAllCharacterSprites()
        {
            string[] folders = new string[]
            {
                "Assets/_Project/Art/Characters/Rimba/Scene1",
                "Assets/_Project/Art/Characters/Abah/Scene1"
            };

            int count = 0;
            foreach (string folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 100;
                        importer.filterMode = FilterMode.Point;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureImportHelper] Configured {count} character sprites as Sprite (2D and UI)");
        }

        [MenuItem("Main Karinding/Setup/Configure All Environment Backgrounds")]
        public static void ConfigureAllBackgrounds()
        {
            string[] folders = new string[]
            {
                "Assets/_Project/Art/Environment/Chapter1/Scene1_TerasRumah",
                "Assets/_Project/Art/Environment/Chapter1/Scene1_Exploration"
            };

            int count = 0;
            foreach (string folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 100;
                        importer.filterMode = FilterMode.Point;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureImportHelper] Configured {count} background sprites as Sprite (2D and UI)");
        }

        [MenuItem("Main Karinding/Setup/Configure All UI Sprites")]
        public static void ConfigureAllUISprites()
        {
            string[] folders = new string[]
            {
                "Assets/_Project/Art/UI/Dialogue",
                "Assets/_Project/Art/UI/ActionButtons",
                "Assets/_Project/Art/UI/Analog"
            };

            int count = 0;
            foreach (string folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 100;
                        importer.filterMode = FilterMode.Point;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureImportHelper] Configured {count} UI sprites as Sprite (2D and UI)");
        }
    }
}
