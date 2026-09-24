using UnityEngine;
using UnityEngine.UI;
using System;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Strict interaction button for cutscene Beat 5.
    /// MUST be tapped before cutscene can continue (no timeout/fallback).
    /// </summary>
    public class CutsceneActionButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject buttonRoot;
        [SerializeField] private Button actionButton;
        [SerializeField] private GameObject tapIndicator;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip tapConfirmClip;

        /// <summary>
        /// Event permanen: di-subscribe sekali (misal di Start controller).
        /// Tidak bisa ke-null oleh Hide()/Show() — tidak ada lagi callback bisu.
        /// </summary>
        public event Action pressed;

        private bool isActive = false;

        private void Awake()
        {
            if (buttonRoot != null)
                buttonRoot.SetActive(false);

            if (actionButton != null)
                actionButton.onClick.AddListener(OnButtonClick);
        }

        /// <summary>
        /// Show action button and wait for player tap.
        /// Callback didaftarkan via event <see cref="pressed"/>, bukan parameter.
        /// </summary>
        public void Show()
        {
            isActive = true;

            if (buttonRoot != null)
                buttonRoot.SetActive(true);

            if (tapIndicator != null)
                tapIndicator.SetActive(true);
        }

        /// <summary>
        /// Show + callback lama (deprecated, dipertahankan biar tidak jebol bila masih dipanggil).
        /// </summary>
        [Obsolete("Pakai event pressed + Show() tanpa argumen.")]
        public void Show(Action onPressed)
        {
            if (onPressed != null)
                pressed += onPressed;
            Show();
        }

        /// <summary>
        /// Hide action button immediately.
        /// </summary>
        public void Hide()
        {
            isActive = false;

            if (buttonRoot != null)
                buttonRoot.SetActive(false);

            if (tapIndicator != null)
                tapIndicator.SetActive(false);
        }

        private void OnButtonClick()
        {
            if (!isActive) return;

            Debug.Log("[CutsceneActionButton] Lihat tapped -> Beat 6");

            // Play SFX
            if (sfxSource != null && tapConfirmClip != null)
                sfxSource.PlayOneShot(tapConfirmClip);

            // Hide button
            Hide();

            // Invoke permanen: teriak kalau tidak ada subscriber
            if (pressed == null)
                Debug.LogError("[CutsceneActionButton] pressed tanpa subscriber! Pastikan controller subscribe di Start.");
            else
                pressed.Invoke();
        }

        private void OnDestroy()
        {
            if (actionButton != null)
                actionButton.onClick.RemoveListener(OnButtonClick);
        }
    }
}
