using System.Collections;
using UnityEngine;
using MainKarinding.Core;

/// <summary>
/// Scene 6 — kembali ke Teras setelah assembly selesai.
/// SuccessOverlay diberi waktu tampil dulu, baru LoadScene Teras (EntryPoint Scene6 -> closing).
/// </summary>
[AddComponentMenu("Main Karinding/Crafting/Scene6 Assembly Return")]
public class Scene6AssemblyReturn : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string terasSceneName = "Ch1_TerasRumahKebun";
    [SerializeField] private float delayAfterComplete = 2.5f;

    private CraftingAssemblyController controller;
    private bool returning;

    private void OnEnable()
    {
        if (controller == null) controller = Object.FindFirstObjectByType<CraftingAssemblyController>();
        if (controller != null) controller.OnAssemblyComplete += OnComplete;
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnAssemblyComplete -= OnComplete;
    }

    private void OnComplete()
    {
        if (returning) return;
        returning = true;
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        yield return new WaitForSeconds(delayAfterComplete);
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
            yield break;
        }
        Debug.Log("[Scene6Return] Assembly selesai -> balik Teras (closing S6).");
        UnityEngine.SceneManagement.SceneManager.LoadScene(terasSceneName);
    }
}
