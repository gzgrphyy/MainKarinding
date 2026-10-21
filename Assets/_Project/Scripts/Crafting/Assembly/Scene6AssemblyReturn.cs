using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MainKarinding.Core;

/// <summary>
/// Scene 6 — kembali ke Teras setelah assembly selesai.
/// Menunggu player klik tombol lanjut (SuccessOverlay/Panel/BtnLanjut), baru LoadScene Teras (EntryPoint Scene6 -> closing).
/// </summary>
[AddComponentMenu("Main Karinding/Crafting/Scene6 Assembly Return")]
public class Scene6AssemblyReturn : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string terasSceneName = "Ch1_TerasRumahKebun";
    [SerializeField] private string continueButtonName = "BtnLanjut";
    [SerializeField] private float delayAfterClick = 0f;
    [Tooltip("Fallback saja: dipakai kalau tombol lanjut tidak ditemukan di scene.")]
    [SerializeField] private float delayAfterComplete = 2.5f;

    private CraftingAssemblyController controller;
    private bool returning;
    private Button continueButton;
    private bool continueClicked;

    private void OnEnable()
    {
        if (controller == null) controller = Object.FindFirstObjectByType<CraftingAssemblyController>();
        if (controller != null) controller.OnAssemblyComplete += OnComplete;
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnAssemblyComplete -= OnComplete;
        RemoveContinueListener();
    }

    private void OnComplete()
    {
        if (returning) return;
        returning = true;
        StartCoroutine(WaitForContinueRoutine());
    }

    private IEnumerator WaitForContinueRoutine()
    {
        continueButton = FindContinueButton();
        if (continueButton == null)
        {
            Debug.LogWarning($"[Scene6Return] Tombol '{continueButtonName}' tidak ditemukan — fallback auto-lanjut {delayAfterComplete}s.");
            yield return new WaitForSeconds(delayAfterComplete);
            ReturnToTeras();
            yield break;
        }

        continueClicked = false;
        continueButton.onClick.RemoveListener(OnContinueClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        Debug.Log($"[Scene6Return] Assembly selesai -> menunggu player klik '{continueButtonName}'.");
        while (!continueClicked) yield return null;
        RemoveContinueListener();
        if (delayAfterClick > 0f) yield return new WaitForSeconds(delayAfterClick);
        ReturnToTeras();
    }

    private Button FindContinueButton()
    {
        var all = Resources.FindObjectsOfTypeAll<Button>();
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var b in all)
        {
            if (b == null || b.gameObject.name != continueButtonName) continue;
            if (b.gameObject.scene != activeScene) continue;
            return b;
        }
        return null;
    }

    private void OnContinueClicked()
    {
        continueClicked = true;
    }

    private void RemoveContinueListener()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void ReturnToTeras()
    {
        StoryState.EnsureExists();
        if (StoryState.Instance != null) StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene6);
        int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        bool exists = false;
        for (int i = 0; i < count; i++)
        {
            if (UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i).Contains(terasSceneName))
            { exists = true; break; }
        }
        if (!exists)
        {
            Debug.LogError($"[Scene6Return] Scene '{terasSceneName}' tidak ada di Build Settings!");
            return;
        }
        Debug.Log("[Scene6Return] Lanjut diklik -> balik Teras (closing S6).");
        UnityEngine.SceneManagement.SceneManager.LoadScene(terasSceneName);
    }
}
