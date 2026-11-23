using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarindingLegacy.UI
{
    public enum GameLanguage
    {
        Indonesian = 0,
        Sundanese = 1
    }

    /// <summary>
    /// Pusat teks UI untuk bahasa Indonesia dan Sunda. Pilihan bahasa disimpan di PlayerPrefs
    /// sehingga tetap tersimpan saat game dibuka kembali.
    /// </summary>
    public static class LocalizationManager
    {
        public const string KEY_LANGUAGE = "Karinding_Language";

        public static event Action LanguageChanged;

        private static bool initialized;
        private static GameLanguage currentLanguage = GameLanguage.Indonesian;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
        }

        public static void Initialize()
        {
            if (initialized) return;

            initialized = true;
            int saved = PlayerPrefs.GetInt(KEY_LANGUAGE, (int)GameLanguage.Indonesian);
            currentLanguage = saved == (int)GameLanguage.Sundanese
                ? GameLanguage.Sundanese
                : GameLanguage.Indonesian;
        }

        public static void SetLanguage(GameLanguage language)
        {
            EnsureInitialized();

            GameLanguage next = language == GameLanguage.Sundanese
                ? GameLanguage.Sundanese
                : GameLanguage.Indonesian;

            bool changed = currentLanguage != next;
            currentLanguage = next;

            PlayerPrefs.SetInt(KEY_LANGUAGE, (int)currentLanguage);
            PlayerPrefs.Save();

            if (changed)
            {
                LanguageChanged?.Invoke();
            }
        }

        public static void SetLanguage(int languageIndex)
        {
            SetLanguage(languageIndex == (int)GameLanguage.Sundanese
                ? GameLanguage.Sundanese
                : GameLanguage.Indonesian);
        }

        public static string Get(string key)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key)) return string.Empty;

            if (Entries.TryGetValue(key, out string[] values))
            {
                int index = currentLanguage == GameLanguage.Sundanese ? 1 : 0;
                if (index < values.Length) return values[index];
                return values[0];
            }

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        private static void EnsureInitialized()
        {
            if (!initialized) Initialize();
        }

        private static readonly Dictionary<string, string[]> Entries = new Dictionary<string, string[]>
        {
            { "common.back", new[] { "KEMBALI", "BALIK" } },

            { "main.play", new[] { "MULAI", "MIMITIAN" } },
            { "main.settings", new[] { "PENGATURAN", "PANGATURAN" } },
            { "main.credits", new[] { "TENTANG", "NGEUNAAN" } },
            { "main.quit", new[] { "KELUAR", "KALUAR" } },

            { "play.title", new[] { "MULAI PERMAINAN", "MIMITIAN KAULINAN" } },
            { "play.newGame", new[] { "GAME BARU", "KAULINAN ANYAR" } },
            { "play.continue", new[] { "LANJUTKAN", "TERUSKEUN" } },

            { "save.title", new[] { "PILIH SLOT PENYIMPANAN", "PILIH SLOT NYIMPEN" } },
            { "save.titleNew", new[] { "PILIH SLOT - GAME BARU", "PILIH SLOT - KAULINAN ANYAR" } },
            { "save.titleContinue", new[] { "LANJUTKAN GAME", "TERUSKEUN KAULINAN" } },
            { "save.subtitleNewEmpty", new[] { "Hanya slot kosong yang dapat dipilih untuk memulai game baru:", "Ngan slot kosong nu bisa dipilih pikeun ngamimitian kaulinan anyar:" } },
            { "save.subtitleNewFull", new[] { "Semua slot sudah penuh. Tidak ada slot kosong untuk game baru.", "Sadaya slot tos pinuh. Teu aya slot kosong pikeun kaulinan anyar." } },
            { "save.subtitleContinue", new[] { "Pilih slot progres permainan yang ingin Anda lanjutkan:", "Pilih slot kamajuan kaulinan anu badé diteruskeun:" } },
            { "save.subtitleSelectEmpty", new[] { "Pilih slot kosong untuk memulai petualangan baru:", "Pilih slot kosong pikeun ngamimitian petualangan anyar:" } },
            { "save.noSave", new[] { "Belum ada save game", "Teu acan aya simpenan kaulinan" } },
            { "save.corruptSlotNew", new[] { "Data pada slot ini rusak dan tidak dapat digunakan.", "Data dina slot ieu rusak jeung teu bisa dipaké." } },
            { "save.slotFullChooseEmpty", new[] { "Slot sudah terisi dan tidak dapat ditimpa. Pilih slot kosong.", "Slot tos eusian jeung teu bisa ditimpa. Pilih slot kosong." } },
            { "save.corruptLoad", new[] { "Data save rusak dan tidak dapat dimuat.", "Data simpenan rusak jeung teu bisa dimuat." } },
            { "save.loadFailed", new[] { "Gagal memuat save game. Coba lagi.", "Gagal ngamuat simpenan kaulinan. Cobaan deui." } },
            { "save.invalidSlot", new[] { "Slot tidak valid.", "Slot teu valid." } },
            { "save.slotFullNoOverwrite", new[] { "Slot sudah terisi dan tidak dapat ditimpa.", "Slot tos eusian jeung teu bisa ditimpa." } },
            { "save.createFailed", new[] { "Gagal membuat save baru. Coba lagi.", "Gagal nyieun simpenan anyar. Cobaan deui." } },

            { "confirm.title", new[] { "KONFIRMASI", "KONFIRMASI" } },
            { "confirm.message", new[] { "Mulai game baru pada slot ini?\n\nSlot {0} akan diisi dengan progres baru.", "Mimitian kaulinan anyar dina slot ieu?\n\nSlot {0} bakal dieusian ku kamajuan anyar." } },
            { "confirm.yes", new[] { "YA, MULAI", "ENYA, MIMITIAN" } },
            { "confirm.cancel", new[] { "BATAL", "BOLAY" } },

            { "settings.title", new[] { "PENGATURAN", "PANGATURAN" } },
            { "settings.master", new[] { "Volume Master", "Sora Master" } },
            { "settings.music", new[] { "Musik Latar", "Musik Pangiring" } },
            { "settings.sfx", new[] { "Efek Suara", "Éfék Sora" } },
            { "settings.language", new[] { "Bahasa", "Basa" } },

            { "credits.title", new[] { "TENTANG GAME", "NGEUNAAN KAULINAN" } },
            { "credits.body", new[]
                {
                    "KARINDING LEGACY\n\n" +
                    "Karya game interaktif yang mengangkat seni dan kearifan instrumen tradisional bambu Nusantara: Karinding.\n\n" +
                    "• Engine: Unity 6 (Universal Render Pipeline)\n" +
                    "• Visual & Artwork: Karinding Legacy Art Pack\n" +
                    "• Suara & Audio: Pelestarian Musik Etnik Tradisional\n\n" +
                    "Kembangkan harmoni nada, lestarikan warisan budaya.",
                    "KARINDING LEGACY\n\n" +
                    "Karya kaulinan interaktif anu ngangkat seni jeung kearifan instrumen tradisional awi Nusantara: Karinding.\n\n" +
                    "• Engine: Unity 6 (Universal Render Pipeline)\n" +
                    "• Visual & Artwork: Karinding Legacy Art Pack\n" +
                    "• Sora & Audio: Pelestarian Musik Étnik Tradisional\n\n" +
                    "Kembangkeun harmoni nada, lestarikeun warisan budaya."
                } },

            { "slot.loading", new[] { "Memuat data slot...", "Ngamuat data slot..." } },
            { "slot.title", new[] { "SLOT {0}", "SLOT {0}" } },
            { "slot.corruptNew", new[] { "DATA RUSAK - Tidak dapat digunakan", "DATA RUSAK - Teu bisa dipaké" } },
            { "slot.filledNew", new[] { "{0} - Level {1} - Terisi", "{0} - Level {1} - Eusian" } },
            { "slot.emptyNew", new[] { "KOSONG - Klik untuk mulai game baru", "KOSONG - Klik pikeun ngamimitian kaulinan anyar" } },
            { "slot.corruptLoad", new[] { "DATA RUSAK - Slot tidak dapat dimuat", "DATA RUSAK - Slot teu bisa dimuat" } },
            { "slot.filledLoad", new[] { "{0} - Level {1} - {2}", "{0} - Level {1} - {2}" } },
            { "slot.empty", new[] { "KOSONG", "KOSONG" } }
        };
    }
}
