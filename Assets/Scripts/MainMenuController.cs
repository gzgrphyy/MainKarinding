using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using KarindingLegacy.Save;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Controller utama yang mengelola interaksi Main Menu, pemilihan mode (Game Baru / Lanjutkan),
    /// pemilihan slot penyimpanan, konfirmasi Game Baru, transisi panel, pengaturan audio/layar,
    /// dan transisi ke gameplay.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [Tooltip("Nama scene gameplay yang dituju saat tombol Play ditekan")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Tooltip("Scene opening cutscene yang langsung diputar setelah slot kosong dipilih untuk Game Baru")]
        [SerializeField] private string openingCutsceneSceneName = "Opening_Cutscene";

        [Header("Main Menu View")]
        [SerializeField] private GameObject mainMenuView;
        [SerializeField] private UIPageTransition mainMenuTransition;
        [SerializeField] private GameObject gameLogo;
        [SerializeField] private GameObject gameLogoFrame;
        [SerializeField] private GameObject mainMenuButtonsContainer;
        [SerializeField] private GameObject titleDivider;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Play Mode Selection Panel")]
        [SerializeField] private GameObject playSelectPanel;
        [SerializeField] private UIModalTransition playSelectTransition;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button playSelectCloseButton;

        [Header("Save Slot Selection Panel")]
        [SerializeField] private GameObject saveSlotPanel;
        [SerializeField] private UIModalTransition saveSlotTransition;
        [SerializeField] private TMP_Text saveSlotTitleText;
        [SerializeField] private TMP_Text saveSlotSubtitleText;
        [SerializeField] private SaveSlotUIItem[] slotItems;
        [SerializeField] private Button saveSlotCloseButton;

        [Header("New Game Confirmation Panel")]
        [SerializeField] private GameObject newGameConfirmPanel;
        [SerializeField] private UIModalTransition newGameConfirmTransition;
        [SerializeField] private TMP_Text newGameConfirmText;
        [SerializeField] private Button newGameConfirmYesButton;
        [SerializeField] private Button newGameConfirmCancelButton;

        [Header("Settings Panel UI")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private UIModalTransition settingsTransition;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button languageIndonesianButton;
        [SerializeField] private Button languageSundaneseButton;
        [SerializeField] private TMP_Text languageIndonesianText;
        [SerializeField] private TMP_Text languageSundaneseText;

        [Header("Credits Panel UI")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private UIModalTransition creditsTransition;
        [SerializeField] private Button creditsCloseButton;

        [Header("Audio Feedback (Opsional)")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickClip;

        // PlayerPrefs Keys
        private const string KEY_MASTER_VOL = "Karinding_MasterVolume";
        private const string KEY_MUSIC_VOL = "Karinding_MusicVolume";
        private const string KEY_SFX_VOL = "Karinding_SFXVolume";

        private bool isNewGameMode = true;
        private int pendingNewGameSlot = -1;

        private void Awake()
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
            }

            LocalizationManager.Initialize();
            LoadAndApplySettings();
            BindButtonEvents();
            RefreshLanguageButtons();
        }

        private void Start()
        {
            SetMainMenuVisible(true);
            SetPlayModeHeader(false);
            if (playSelectPanel != null) playSelectPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
            if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        private void Update()
        {
            if (IsEscapePressed())
            {
                if (newGameConfirmPanel != null && newGameConfirmPanel.activeSelf)
                {
                    CancelNewGameConfirmation();
                }
                else if (saveSlotPanel != null && saveSlotPanel.activeSelf)
                {
                    CloseSaveSlots();
                }
                else if (playSelectPanel != null && playSelectPanel.activeSelf)
                {
                    ClosePlaySelect();
                }
                else if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else if (creditsPanel != null && creditsPanel.activeSelf)
                {
                    CloseCredits();
                }
            }
        }

        private bool IsEscapePressed()
        {
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    return UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
                }
            }
            catch
            {
                // Fallback jika New Input System belum aktif
            }

            return false;
        }

        private void LoadAndApplySettings()
        {
            float master = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
            float music = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
            float sfx = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);

            AudioListener.volume = master;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = master;
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }
            if (masterVolumeText != null) masterVolumeText.text = Mathf.RoundToInt(master * 100f) + "%";

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = music;
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }
            if (musicVolumeText != null) musicVolumeText.text = Mathf.RoundToInt(music * 100f) + "%";

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = sfx;
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            if (sfxVolumeText != null) sfxVolumeText.text = Mathf.RoundToInt(sfx * 100f) + "%";
        }

        private void BindButtonEvents()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (playSelectCloseButton != null) playSelectCloseButton.onClick.AddListener(ClosePlaySelect);

            if (saveSlotCloseButton != null) saveSlotCloseButton.onClick.AddListener(CloseSaveSlots);

            if (newGameConfirmYesButton != null) newGameConfirmYesButton.onClick.AddListener(ConfirmNewGame);
            if (newGameConfirmCancelButton != null) newGameConfirmCancelButton.onClick.AddListener(CancelNewGameConfirmation);

            if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(CloseSettings);
            if (creditsCloseButton != null) creditsCloseButton.onClick.AddListener(CloseCredits);

            if (languageIndonesianButton != null) languageIndonesianButton.onClick.AddListener(() => OnLanguageSelected(GameLanguage.Indonesian));
            if (languageSundaneseButton != null) languageSundaneseButton.onClick.AddListener(() => OnLanguageSelected(GameLanguage.Sundanese));
        }

        // ==================== ALUR TOMBOL PLAY & SAVE SLOTS ====================

        public void OnPlayClicked()
        {
            PlaySound();
            OpenPlaySelect();
        }

        public void OpenPlaySelect()
        {
            HideNewGameConfirmation();
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);

            SetPlayModeHeader(true);

            if (playSelectPanel != null)
            {
                if (playSelectTransition != null) playSelectTransition.Show();
                else playSelectPanel.SetActive(true);
            }
        }

        public void ClosePlaySelect()
        {
            PlaySound();
            HideNewGameConfirmation();

            if (playSelectPanel != null)
            {
                if (playSelectTransition != null) playSelectTransition.Hide();
                else playSelectPanel.SetActive(false);
            }

            SetPlayModeHeader(false);
        }

        public void OnNewGameClicked()
        {
            PlaySound();
            isNewGameMode = true;
            pendingNewGameSlot = -1;
            OpenSaveSlots();
        }

        public void OnContinueClicked()
        {
            PlaySound();
            isNewGameMode = false;
            pendingNewGameSlot = -1;
            OpenSaveSlots();
        }

        public void OpenSaveSlots()
        {
            HideNewGameConfirmation();
            SetPlayModeHeader(true);
            if (playSelectPanel != null)
            {
                if (playSelectTransition != null) playSelectTransition.Hide();
                else playSelectPanel.SetActive(false);
            }

            if (saveSlotPanel != null)
            {
                if (saveSlotTitleText != null)
                {
                    saveSlotTitleText.text = LocalizationManager.Get(isNewGameMode ? "save.titleNew" : "save.titleContinue");
                }

                RefreshAllSlots();
                UpdateSaveSlotMessage();

                if (saveSlotTransition != null) saveSlotTransition.Show();
                else saveSlotPanel.SetActive(true);
            }
        }

        public void CloseSaveSlots()
        {
            PlaySound();
            HideNewGameConfirmation();

            if (saveSlotPanel != null)
            {
                if (saveSlotTransition != null) saveSlotTransition.Hide();
                else saveSlotPanel.SetActive(false);
            }

            OpenPlaySelect();
        }

        public void RefreshAllSlots()
        {
            if (slotItems != null)
            {
                for (int i = 0; i < slotItems.Length; i++)
                {
                    if (slotItems[i] != null)
                    {
                        slotItems[i].RefreshDisplay(isNewGameMode, OnSlotSelected, OnSlotDeleted);
                    }
                }
            }
        }

        public void OnSlotSelected(int slotIndex)
        {
            if (!SaveSystem.IsValidSlot(slotIndex)) return;

            PlaySound();
            SaveSlotData data = SaveSystem.GetSlot(slotIndex);

            if (isNewGameMode)
            {
                if (data.hasData)
                {
                    if (saveSlotSubtitleText != null)
                    {
                        saveSlotSubtitleText.text = LocalizationManager.Get(data.isCorrupt
                            ? "save.corruptSlotNew"
                            : "save.slotFullChooseEmpty");
                    }
                    RefreshAllSlots();
                    return;
                }

                CreateNewGameAndPlayOpening(slotIndex);
            }
            else
            {
                if (!data.CanLoad)
                {
                    if (saveSlotSubtitleText != null)
                    {
                        saveSlotSubtitleText.text = LocalizationManager.Get(data.isCorrupt
                            ? "save.corruptLoad"
                            : "save.noSave");
                    }
                    RefreshAllSlots();
                    return;
                }

                if (SaveSystem.LoadGame(slotIndex))
                {
                    LaunchGameplayScene();
                }
                else
                {
                    if (saveSlotSubtitleText != null)
                    {
                        saveSlotSubtitleText.text = LocalizationManager.Get("save.loadFailed");
                    }
                }
            }
        }

        public void OnSlotDeleted(int slotIndex)
        {
            PlaySound();
            SaveSystem.DeleteSave(slotIndex);
            RefreshAllSlots();
            UpdateSaveSlotMessage();
        }

        private void ShowNewGameConfirmation(int slotIndex)
        {
            pendingNewGameSlot = slotIndex;

            if (newGameConfirmText != null)
            {
                newGameConfirmText.text = LocalizationManager.Format("confirm.message", slotIndex);
            }

            if (newGameConfirmPanel != null)
            {
                if (newGameConfirmTransition != null) newGameConfirmTransition.Show();
                else newGameConfirmPanel.SetActive(true);
            }
        }

        public void ConfirmNewGame()
        {
            PlaySound();

            int slotIndex = pendingNewGameSlot;
            pendingNewGameSlot = -1;

            if (!SaveSystem.IsValidSlot(slotIndex))
            {
                HideNewGameConfirmation();
                if (saveSlotSubtitleText != null) saveSlotSubtitleText.text = LocalizationManager.Get("save.invalidSlot");
                return;
            }

            if (SaveSystem.HasSave(slotIndex))
            {
                HideNewGameConfirmation();
                if (saveSlotSubtitleText != null) saveSlotSubtitleText.text = LocalizationManager.Get("save.slotFullNoOverwrite");
                RefreshAllSlots();
                return;
            }

            CreateNewGameAndPlayOpening(slotIndex);
        }

        private void CreateNewGameAndPlayOpening(int slotIndex)
        {
            if (!SaveSystem.IsValidSlot(slotIndex)) return;

            if (SaveSystem.HasSave(slotIndex))
            {
                HideNewGameConfirmation();
                if (saveSlotSubtitleText != null) saveSlotSubtitleText.text = LocalizationManager.Get("save.slotFullNoOverwrite");
                RefreshAllSlots();
                return;
            }

            if (!SaveSystem.CreateNewGame(slotIndex))
            {
                HideNewGameConfirmation();
                if (saveSlotSubtitleText != null) saveSlotSubtitleText.text = LocalizationManager.Get("save.createFailed");
                RefreshAllSlots();
                return;
            }

            HideNewGameConfirmation();
            LaunchOpeningCutscene();
        }

        public void CancelNewGameConfirmation()
        {
            PlaySound();
            pendingNewGameSlot = -1;
            HideNewGameConfirmation();
            RefreshAllSlots();

            if (saveSlotSubtitleText != null)
            {
                saveSlotSubtitleText.text = LocalizationManager.Get(isNewGameMode
                    ? "save.subtitleSelectEmpty"
                    : "save.subtitleContinue");
            }
        }

        private void HideNewGameConfirmation()
        {
            if (newGameConfirmPanel == null) return;

            if (newGameConfirmTransition != null) newGameConfirmTransition.Hide();
            else newGameConfirmPanel.SetActive(false);
        }

        private void UpdateSaveSlotMessage()
        {
            if (saveSlotSubtitleText == null) return;

            int occupied = SaveSystem.CountOccupiedSlots();

            if (!isNewGameMode && occupied == 0)
            {
                saveSlotSubtitleText.text = LocalizationManager.Get("save.noSave");
            }
            else if (isNewGameMode && occupied >= SaveSystem.SLOT_COUNT)
            {
                saveSlotSubtitleText.text = LocalizationManager.Get("save.subtitleNewFull");
            }
            else if (isNewGameMode)
            {
                saveSlotSubtitleText.text = LocalizationManager.Get("save.subtitleNewEmpty");
            }
            else
            {
                saveSlotSubtitleText.text = LocalizationManager.Get("save.subtitleContinue");
            }
        }

        private void LaunchOpeningCutscene()
        {
            Debug.Log($"[MainMenuController] Memuat scene opening cutscene: '{openingCutsceneSceneName}'");

            if (string.IsNullOrEmpty(openingCutsceneSceneName) ||
                !Application.CanStreamedLevelBeLoaded(openingCutsceneSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene opening cutscene '{openingCutsceneSceneName}' belum ada di Build Settings.");
                return;
            }

            SceneManager.LoadScene(openingCutsceneSceneName);
        }

        private void LaunchGameplayScene()
        {
            Debug.Log($"[MainMenuController] Memuat scene gameplay: '{gameplaySceneName}'");

            if (string.IsNullOrEmpty(gameplaySceneName)) return;

            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenuController] Scene '{gameplaySceneName}' belum ada di Build Settings. Memuat scene index 1.");
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
                else
                {
                    Debug.LogError($"[MainMenuController] Scene '{gameplaySceneName}' tidak dapat dimuat!");
                }
            }
        }

        // ==================== PANEL SETTINGS & CREDITS ====================

        public void OpenSettings()
        {
            PlaySound();
            CloseAllModals();

            if (settingsPanel != null)
            {
                if (settingsTransition != null) settingsTransition.Show();
                else settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            PlaySound();
            PlayerPrefs.Save();

            if (settingsPanel != null)
            {
                if (settingsTransition != null) settingsTransition.Hide();
                else settingsPanel.SetActive(false);
            }
        }

        public void OnLanguageSelected(GameLanguage language)
        {
            PlaySound();
            LocalizationManager.SetLanguage(language);
            RefreshLanguageButtons();
        }

        private void RefreshLanguageButtons()
        {
            bool isIndonesian = LocalizationManager.CurrentLanguage == GameLanguage.Indonesian;
            ApplyLanguageButtonVisual(languageIndonesianText, "INDONESIA", isIndonesian);
            ApplyLanguageButtonVisual(languageSundaneseText, "SUNDA", !isIndonesian);
        }

        private static void ApplyLanguageButtonVisual(TMP_Text label, string languageName, bool selected)
        {
            if (label == null) return;

            label.text = selected ? "> " + languageName : languageName;
            label.color = selected
                ? new Color(0.98f, 0.86f, 0.48f)
                : new Color(0.68f, 0.64f, 0.58f);
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        public void OpenCredits()
        {
            PlaySound();
            CloseAllModals();

            if (creditsPanel != null)
            {
                if (creditsTransition != null) creditsTransition.Show();
                else creditsPanel.SetActive(true);
            }
        }

        public void CloseCredits()
        {
            PlaySound();

            if (creditsPanel != null)
            {
                if (creditsTransition != null) creditsTransition.Hide();
                else creditsPanel.SetActive(false);
            }
        }

        private void CloseAllModals()
        {
            pendingNewGameSlot = -1;
            if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
            if (playSelectPanel != null) playSelectPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void SetPlayModeHeader(bool inPlayMode)
        {
            if (gameLogoFrame != null)
            {
                gameLogoFrame.SetActive(inPlayMode);
            }

            if (mainMenuButtonsContainer != null)
            {
                mainMenuButtonsContainer.SetActive(!inPlayMode);
            }

            if (titleDivider != null)
            {
                titleDivider.SetActive(!inPlayMode);
            }

            // Fallback otomatis mencari referensi komponen jika belum tersambung di Inspector
            if (mainMenuView != null)
            {
                if (mainMenuButtonsContainer == null)
                {
                    Transform btnContainer = mainMenuView.transform.Find("ButtonsContainer");
                    if (btnContainer != null) btnContainer.gameObject.SetActive(!inPlayMode);
                }

                if (titleDivider == null)
                {
                    Transform div = mainMenuView.transform.Find("TitleDivider");
                    if (div != null) div.gameObject.SetActive(!inPlayMode);
                }

                if (gameLogoFrame == null)
                {
                    Transform frame = mainMenuView.transform.Find("GameLogoFrame");
                    if (frame != null) frame.gameObject.SetActive(inPlayMode);
                }
            }
        }

        private void SetMainMenuVisible(bool isVisible)
        {
            if (mainMenuTransition != null)
            {
                mainMenuTransition.SetVisibleImmediate(isVisible);
                return;
            }

            if (mainMenuView != null) mainMenuView.SetActive(isVisible);
        }

        private void AnimateMainMenu(bool isVisible)
        {
            if (mainMenuTransition != null)
            {
                if (isVisible) mainMenuTransition.Show();
                else mainMenuTransition.Hide();
                return;
            }

            SetMainMenuVisible(isVisible);
        }

        // ==================== AUDIO & DISPLAY SETTINGS ====================

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, value);
            if (masterVolumeText != null) masterVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        public void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, value);
            if (musicVolumeText != null) musicVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        public void SetSFXVolume(float value)
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOL, value);
            if (sfxVolumeText != null) sfxVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        public void QuitGame()
        {
            PlaySound();
            Debug.Log("[MainMenuController] Menutup game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlaySound()
        {
            if (uiAudioSource != null && buttonClickClip != null)
            {
                uiAudioSource.PlayOneShot(buttonClickClip);
            }
        }
    }
}
