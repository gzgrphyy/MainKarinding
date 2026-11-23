using System;
using UnityEngine;

namespace KarindingLegacy.Save
{
    /// <summary>
    /// Data struktur untuk satu slot penyimpanan game.
    /// </summary>
    [Serializable]
    public class SaveSlotData
    {
        public int slotIndex;
        public bool hasData;
        public bool isCorrupt;
        public string chapterName;
        public string saveDate;
        public string playTime;
        public int level;
        public string sceneName;

        public bool CanLoad => hasData && !isCorrupt;
    }

    /// <summary>
    /// Mengelola penyimpanan, pemuatan, dan status 6 slot save game.
    /// Data disimpan secara lokal dan persisten melalui PlayerPrefs.
    /// </summary>
    public static class SaveSystem
    {
        public const int SLOT_COUNT = 6;
        public const string ACTIVE_SLOT_KEY = "Karinding_ActiveSaveSlot";
        public const string START_MODE_KEY = "Karinding_GameStartMode"; // "New" atau "Continue"

        private const string KEY_EXISTS_PREFIX = "Karinding_SaveSlot_Exists_";
        private const string KEY_DATE_PREFIX = "Karinding_SaveSlot_Date_";
        private const string KEY_CHAPTER_PREFIX = "Karinding_SaveSlot_Chapter_";
        private const string KEY_PLAYTIME_PREFIX = "Karinding_SaveSlot_PlayTime_";
        private const string KEY_LEVEL_PREFIX = "Karinding_SaveSlot_Level_";
        private const string KEY_SCENE_PREFIX = "Karinding_SaveSlot_Scene_";

        public static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 1 && slotIndex <= SLOT_COUNT;
        }

        public static bool HasSave(int slotIndex)
        {
            return IsValidSlot(slotIndex) &&
                   PlayerPrefs.GetInt(KEY_EXISTS_PREFIX + slotIndex, 0) == 1;
        }

        public static SaveSlotData GetSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                return new SaveSlotData
                {
                    slotIndex = slotIndex,
                    hasData = false,
                    isCorrupt = true,
                    chapterName = "Slot tidak valid",
                    saveDate = "-",
                    playTime = "-",
                    level = 0,
                    sceneName = string.Empty
                };
            }

            bool exists = HasSave(slotIndex);
            if (!exists)
            {
                return new SaveSlotData
                {
                    slotIndex = slotIndex,
                    hasData = false,
                    isCorrupt = false,
                    chapterName = "KOSONG",
                    saveDate = "-",
                    playTime = "-",
                    level = 0,
                    sceneName = string.Empty
                };
            }

            string chapterName = PlayerPrefs.GetString(KEY_CHAPTER_PREFIX + slotIndex, string.Empty).Trim();
            string saveDate = PlayerPrefs.GetString(KEY_DATE_PREFIX + slotIndex, string.Empty).Trim();
            string playTime = PlayerPrefs.GetString(KEY_PLAYTIME_PREFIX + slotIndex, string.Empty).Trim();
            string sceneName = PlayerPrefs.GetString(KEY_SCENE_PREFIX + slotIndex, string.Empty).Trim();
            int level = PlayerPrefs.GetInt(KEY_LEVEL_PREFIX + slotIndex, 0);

            bool hasChapter = PlayerPrefs.HasKey(KEY_CHAPTER_PREFIX + slotIndex);
            bool hasLevel = PlayerPrefs.HasKey(KEY_LEVEL_PREFIX + slotIndex);
            bool isCorrupt = !hasChapter || !hasLevel || string.IsNullOrEmpty(chapterName) || level < 1;

            if (string.IsNullOrEmpty(saveDate)) saveDate = "Tanggal tidak tersedia";
            if (string.IsNullOrEmpty(playTime)) playTime = "00:00:00";
            if (string.IsNullOrEmpty(sceneName)) sceneName = "SampleScene";
            if (level < 1) level = 1;

            return new SaveSlotData
            {
                slotIndex = slotIndex,
                hasData = true,
                isCorrupt = isCorrupt,
                chapterName = isCorrupt ? "DATA RUSAK" : chapterName,
                saveDate = saveDate,
                playTime = playTime,
                level = level,
                sceneName = sceneName
            };
        }

        public static bool CreateNewGame(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[SaveSystem] Slot {slotIndex} tidak valid.");
                return false;
            }

            if (HasSave(slotIndex))
            {
                Debug.LogWarning($"[SaveSystem] Slot {slotIndex} sudah terisi dan tidak boleh ditimpa.");
                return false;
            }

            try
            {
                PlayerPrefs.SetInt(KEY_EXISTS_PREFIX + slotIndex, 1);
                PlayerPrefs.SetString(KEY_DATE_PREFIX + slotIndex, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                PlayerPrefs.SetString(KEY_CHAPTER_PREFIX + slotIndex, "Bab 1: Awal Petualangan");
                PlayerPrefs.SetString(KEY_PLAYTIME_PREFIX + slotIndex, "00:00:00");
                PlayerPrefs.SetInt(KEY_LEVEL_PREFIX + slotIndex, 1);
                PlayerPrefs.SetString(KEY_SCENE_PREFIX + slotIndex, "SampleScene");

                PlayerPrefs.SetInt(ACTIVE_SLOT_KEY, slotIndex);
                PlayerPrefs.SetString(START_MODE_KEY, "New");
                PlayerPrefs.Save();

                Debug.Log($"[SaveSystem] Game baru dibuat di Slot {slotIndex}.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal membuat save Slot {slotIndex}: {ex.Message}");
                return false;
            }
        }

        public static bool LoadGame(int slotIndex)
        {
            SaveSlotData data = GetSlot(slotIndex);
            if (!data.CanLoad)
            {
                Debug.LogWarning($"[SaveSystem] Slot {slotIndex} tidak dapat dimuat.");
                return false;
            }

            try
            {
                PlayerPrefs.SetInt(ACTIVE_SLOT_KEY, slotIndex);
                PlayerPrefs.SetString(START_MODE_KEY, "Continue");
                PlayerPrefs.Save();

                Debug.Log($"[SaveSystem] Memuat Slot {slotIndex}. Mode: Continue.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal memuat Slot {slotIndex}: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteSave(int slotIndex)
        {
            if (!IsValidSlot(slotIndex)) return false;

            PlayerPrefs.DeleteKey(KEY_EXISTS_PREFIX + slotIndex);
            PlayerPrefs.DeleteKey(KEY_DATE_PREFIX + slotIndex);
            PlayerPrefs.DeleteKey(KEY_CHAPTER_PREFIX + slotIndex);
            PlayerPrefs.DeleteKey(KEY_PLAYTIME_PREFIX + slotIndex);
            PlayerPrefs.DeleteKey(KEY_LEVEL_PREFIX + slotIndex);
            PlayerPrefs.DeleteKey(KEY_SCENE_PREFIX + slotIndex);
            PlayerPrefs.Save();

            Debug.Log($"[SaveSystem] Slot {slotIndex} berhasil dihapus.");
            return true;
        }

        public static int CountOccupiedSlots()
        {
            int count = 0;
            for (int i = 1; i <= SLOT_COUNT; i++)
            {
                if (HasSave(i)) count++;
            }
            return count;
        }

        public static int CountEmptySlots()
        {
            return SLOT_COUNT - CountOccupiedSlots();
        }

        public static bool HasAnySave()
        {
            return CountOccupiedSlots() > 0;
        }

        public static bool AreAllSlotsFull()
        {
            return CountEmptySlots() == 0;
        }

        public static int GetFirstEmptySlot()
        {
            for (int i = 1; i <= SLOT_COUNT; i++)
            {
                if (!HasSave(i)) return i;
            }
            return -1;
        }

        public static int GetActiveSlotIndex()
        {
            return PlayerPrefs.GetInt(ACTIVE_SLOT_KEY, -1);
        }

        public static SaveSlotData GetActiveSlotData()
        {
            return GetSlot(GetActiveSlotIndex());
        }

        /// <summary>
        /// Dipanggil oleh gameplay saat progres berubah. Slot aktif harus sudah ada.
        /// </summary>
        public static bool UpdateActiveProgress(int level, string chapterName = null, string playTime = null, string sceneName = null)
        {
            int slotIndex = GetActiveSlotIndex();
            if (!IsValidSlot(slotIndex) || !HasSave(slotIndex)) return false;

            if (level >= 1) PlayerPrefs.SetInt(KEY_LEVEL_PREFIX + slotIndex, level);
            if (!string.IsNullOrEmpty(chapterName)) PlayerPrefs.SetString(KEY_CHAPTER_PREFIX + slotIndex, chapterName);
            if (!string.IsNullOrEmpty(playTime)) PlayerPrefs.SetString(KEY_PLAYTIME_PREFIX + slotIndex, playTime);
            if (!string.IsNullOrEmpty(sceneName)) PlayerPrefs.SetString(KEY_SCENE_PREFIX + slotIndex, sceneName);

            PlayerPrefs.SetString(KEY_DATE_PREFIX + slotIndex, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// Membuat sample save data di slot kosong untuk pengujian menu Lanjutkan.
        /// </summary>
        public static bool SeedSampleSave(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || HasSave(slotIndex)) return false;

            PlayerPrefs.SetInt(KEY_EXISTS_PREFIX + slotIndex, 1);
            PlayerPrefs.SetString(KEY_DATE_PREFIX + slotIndex, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            PlayerPrefs.SetString(KEY_CHAPTER_PREFIX + slotIndex, $"Bab {slotIndex}: Jejak Karinding Kolot");
            PlayerPrefs.SetString(KEY_PLAYTIME_PREFIX + slotIndex, $"00:{slotIndex * 10:00}:30");
            PlayerPrefs.SetInt(KEY_LEVEL_PREFIX + slotIndex, slotIndex);
            PlayerPrefs.SetString(KEY_SCENE_PREFIX + slotIndex, "SampleScene");
            PlayerPrefs.Save();
            return true;
        }
    }
}
