#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using KarindingLegacy.Save;

namespace KarindingLegacy.UI.Editor
{
    /// <summary>
    /// Jendela editor interaktif untuk memvalidasi asset, mengelola scene,
    /// menguji save slots, dan membangun ulang Main Menu dengan satu klik di Unity Editor.
    /// </summary>
    public class MainMenuSetupWindow : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("Tools/Karinding Legacy/Main Menu Setup Window", priority = 0)]
        public static void ShowWindow()
        {
            var win = GetWindow<MainMenuSetupWindow>("Karinding Main Menu");
            win.minSize = new Vector2(500, 640);
            win.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header Banner
            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("KARINDING LEGACY - MAIN MENU SETUP", headerStyle);
            EditorGUILayout.LabelField("Panel Kontrol, Save Slots & Validasi Main Menu", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(12);

            // Section: Asset Validation
            DrawSectionHeader("1. Status Asset Visual");
            CheckAssetItem("Background", "Assets/Asset/backround-new.png");
            CheckAssetItem("Game Logo / Judul", "Assets/Asset/Name Game.png");
            CheckAssetItem("Tombol Mulai (Normal)", "Assets/Asset/04_button_mulai_normal.png");
            CheckAssetItem("Tombol Mulai (Hover)", "Assets/Asset/08_button_mulai_hover.png");
            CheckAssetItem("Tombol Pengaturan (Normal)", "Assets/Asset/05_button_pengaturan_normal.png");
            CheckAssetItem("Tombol Pengaturan (Hover)", "Assets/Asset/09_button_pengaturan_hover.png");
            CheckAssetItem("Tombol Tentang (Normal)", "Assets/Asset/06_button_tentang_normal.png");
            CheckAssetItem("Tombol Tentang (Hover)", "Assets/Asset/10_button_tentang_hover.png");
            CheckAssetItem("Tombol Keluar (Normal)", "Assets/Asset/07_button_keluar_normal.png");
            CheckAssetItem("Tombol Keluar (Hover)", "Assets/Asset/11_button_keluar_hover.png");
            CheckAssetItem("Frame Blank (Modal / Slots)", "Assets/Asset/02_frame_normal_blank.png");
            CheckAssetItem("Frame Title (Modal / Header)", "Assets/Asset/title_frame.png");
            CheckAssetItem("Decorative Divider", "Assets/Asset/20_decorative_divider.png");

            EditorGUILayout.Space(12);

            // Section: Status Save Slots
            DrawSectionHeader("2. Status Save Slots (Penyimpanan Game)");
            for (int i = 1; i <= 3; i++)
            {
                var slotData = SaveSystem.GetSlot(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Slot {i}:", GUILayout.Width(70));
                if (slotData.hasData)
                {
                    GUI.color = new Color(0.96f, 0.82f, 0.44f);
                    EditorGUILayout.LabelField($"{slotData.chapterName} (Lv.{slotData.level}) • {slotData.saveDate}");
                }
                else
                {
                    GUI.color = Color.gray;
                    EditorGUILayout.LabelField("< Kosong >");
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✨ Buat Contoh Save (Slot 1)", GUILayout.Height(24)))
            {
                SaveSystem.SeedSampleSave(1);
                Debug.Log("[MainMenuSetupWindow] Contoh data save dibuat di Slot 1.");
            }
            if (GUILayout.Button("🗑 Reset Semua Save Slot", GUILayout.Height(24)))
            {
                SaveSystem.DeleteSave(1);
                SaveSystem.DeleteSave(2);
                SaveSystem.DeleteSave(3);
                Debug.Log("[MainMenuSetupWindow] Seluruh save slot telah direset.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            // Section: Scenes & Build Settings
            DrawSectionHeader("3. Status Scene & Build Settings");
            CheckFileItem("Scene Main Menu", "Assets/Scenes/MainMenu.unity");
            CheckFileItem("Scene Gameplay (SampleScene)", "Assets/Scenes/SampleScene.unity");
            CheckFileItem("Prefab MainMenuCanvas", "Assets/Prefabs/MainMenuCanvas.prefab");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Daftar Scene di Build Settings:", EditorStyles.boldLabel);
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var s = EditorBuildSettings.scenes[i];
                EditorGUILayout.LabelField($"   [{i}] {s.path} (Aktif: {s.enabled})");
            }

            EditorGUILayout.Space(15);

            // Section: Actions
            DrawSectionHeader("4. Aksi Cepat (Quick Actions)");

            GUI.backgroundColor = new Color(0.96f, 0.78f, 0.35f);
            if (GUILayout.Button("🔨 Bangun Ulang Main Menu Scene (Rebuild)", GUILayout.Height(36)))
            {
                MainMenuBuilder.BuildMainMenuScene(false);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
            if (GUILayout.Button("🧪 Jalankan Pengujian Otomatis (Run Tests)", GUILayout.Height(32)))
            {
                MainMenuIntegrationTest.RunTests(true);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📂 Buka MainMenu.unity", GUILayout.Height(28)))
            {
                MainMenuBuilder.OpenMainMenuScene();
            }
            if (GUILayout.Button("🎮 Buka Gameplay (SampleScene)", GUILayout.Height(28)))
            {
                MainMenuBuilder.OpenGameplayScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (GUILayout.Button(EditorApplication.isPlaying ? "⏹ Hentikan Play Mode" : "▶ Jalankan Game di Editor", GUILayout.Height(30)))
            {
                if (!EditorApplication.isPlaying)
                {
                    if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MainMenu.unity")
                    {
                        MainMenuBuilder.OpenMainMenuScene();
                    }
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(4);
        }

        private void CheckAssetItem(string label, string path)
        {
            bool exists = File.Exists(path);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(220));
            if (exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ Tersedia", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("✗ Hilang", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void CheckFileItem(string label, string path)
        {
            bool exists = File.Exists(path);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(220));
            if (exists)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ Siap", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = new Color(0.9f, 0.6f, 0.1f);
                EditorGUILayout.LabelField("Belum Dibuat", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
