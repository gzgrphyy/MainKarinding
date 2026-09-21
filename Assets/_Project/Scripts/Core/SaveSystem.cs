using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Karinding.Story;
using Karinding.Codex;

namespace Karinding.Core
{
    [Serializable]
    public class AffinitySaveEntry
    {
        public string npcId;
        public int value;
    }

    [Serializable]
    public class SaveData
    {
        public int storyState;
        public List<string> codexUnlockedIds = new List<string>();
        public List<AffinitySaveEntry> affinity = new List<AffinitySaveEntry>();
        public bool showTranslation = true;
        public bool chapter1Complete;
        public int version = 1;
    }

    public static class SaveSystem
    {
        private const string FileName = "KarindingSave.json";
        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save()
        {
            var data = new SaveData();
            // Story
            var storyMgr = UnityEngine.Object.FindFirstObjectByType<StoryManager>();
            if (storyMgr != null) data.storyState = (int)storyMgr.CurrentState;
            else data.storyState = (int)StoryState.Frame_RimbaTertidur;

            // Codex
            var codexMgr = UnityEngine.Object.FindFirstObjectByType<CodexManager>();
            if (codexMgr != null) data.codexUnlockedIds = new List<string>(codexMgr.GetUnlockedIds());
            else data.codexUnlockedIds = new List<string>();

            // Affinity
            var affMgr = UnityEngine.Object.FindFirstObjectByType<AffinityManager>();
            if (affMgr != null)
            {
                data.affinity.Clear();
                foreach (var kv in affMgr.GetAll())
                    data.affinity.Add(new AffinitySaveEntry { npcId = kv.Key, value = kv.Value });
            }

            // Translation pref (from PlayerPrefs, default true)
            data.showTranslation = PlayerPrefs.GetInt("dialogue_show_translation", 1) == 1;

            // Chapter1 complete flag - true if state is KeberangkatanAbah
            data.chapter1Complete = data.storyState >= (int)StoryState.KeberangkatanAbah;

            Save(data);
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SaveSystem] Saved to {FilePath} story:{(StoryState)data.storyState} codex:{data.codexUnlockedIds.Count} affinity:{data.affinity.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e}");
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Debug.Log($"[SaveSystem] No save file at {FilePath}");
                    return null;
                }
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Loaded story:{(StoryState)data.storyState} codex:{data.codexUnlockedIds.Count}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e}");
                return null;
            }
        }

        public static bool HasSave() => File.Exists(FilePath);

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                PlayerPrefs.DeleteKey("dialogue_show_translation");
                Debug.Log("[SaveSystem] Save cleared");
            }
            catch (Exception e) { Debug.LogError($"[SaveSystem] Clear failed: {e}"); }
        }

        public static string GetPath() => FilePath;
    }
}
