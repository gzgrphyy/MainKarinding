using UnityEngine;

/// <summary>
/// Entry produksi minigame crafting TANPA fase search (mencari bambu dihapus).
/// Otomatis saat scene dibuka: langsung tampilkan tombol MULAI MEMOTONG.
/// Alur: scene load -> MULAI MEMOTONG -> Cut 0-50% -> Tap 50-100% -> SuccessOverlay.
/// </summary>
public class CraftingAssemblyStarter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Tuning Cut (0-50%) & Tap (50-100%). Kosong = pakai default di CraftingAssemblyController.")]
    [SerializeField] private CraftingAssemblyConfig assemblyConfig;

    private void Start()
    {
        var ctrl = CraftingAssemblyController.Instance;
        if (ctrl == null) ctrl = FindFirstObjectByType<CraftingAssemblyController>();
        if (ctrl == null)
        {
            Debug.LogError("[CraftingAssemblyStarter] CraftingAssemblyController tidak ada di scene.");
            return;
        }

        if (assemblyConfig != null) ctrl.SetConfig(assemblyConfig);
        ctrl.ShowStartCutButton();
        Debug.Log("[CraftingAssemblyStarter] Scene dibuka -> tombol MULAI MEMOTONG langsung tampil");
    }
}
