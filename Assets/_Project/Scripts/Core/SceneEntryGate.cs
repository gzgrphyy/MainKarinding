using UnityEngine;

namespace MainKarinding.Core
{
    /// <summary>
    /// Gerbang masuk untuk cutscene yang berbagi satu scene file (Ch1_TerasRumahKebun).
    /// Cutscene controller yang di-gate tetap hidup sebagai file .cs, tidak ada penyuntingan
    /// pada script-nya — gate aktif di Awake sehingga Start() controller tidak dipanggil
    /// karena Unity melewati Start() pada component/GameObject yang nonaktif.
    /// </summary>
    [AddComponentMenu("Main Karinding/Cutscenes/Scene Entry Gate")]
    public class SceneEntryGate : MonoBehaviour
    {
        public enum GateMode
        {
            /// <summary>Matikan seluruh subtree GO ini. Pakai kalau tidak ada scene lain yang butuh isinya.</summary>
            DeactivateWholeGameObject = 0,
            /// <summary>Matikan controller + konten, tapi biarkan isi dari list tetap hidup (dipakai scene berikutnya).</summary>
            DeactivateContent = 1,
        }

        [Header("Gate")]
        [Tooltip("Scene yang DICAMPI oleh gate ini. Kalau EntryPoint != nilai ini, gate menutup.")]
        [SerializeField] private Ch1EntryPoint ownEntry = Ch1EntryPoint.Scene1;

        [SerializeField] private GateMode mode = GateMode.DeactivateWholeGameObject;

        [Header("DeactivateWholeGameObject")]
        [Tooltip("Kosongkan — default pakai gameObject milik gate ini.")]
        [SerializeField] private GameObject targetRoot;

        [Header("DeactivateContent")]
        [Tooltip("Controller milik scene ini. Start()-nya tidak akan dipanggil setelah dimatikan.")]
        [SerializeField] private Behaviour[] controllersToDisable = new Behaviour[0];

        [Tooltip("Konten shot/prop milik scene ini, dimatikan agar tidak tampil di atas scene yang aktif.")]
        [SerializeField] private GameObject[] contentToHide = new GameObject[0];

        [Header("Debug")]
        [SerializeField] private bool logOnGate = true;

        private void Awake()
        {
            StoryState.EnsureExists();
            if (StoryState.Instance == null) return;

            bool isMine = StoryState.Instance.EntryPoint == ownEntry;
            if (isMine)
            {
                if (logOnGate) Debug.Log($"[SceneEntryGate] {name}: OPEN (entry={ownEntry})");
                return;
            }

            switch (mode)
            {
                case GateMode.DeactivateWholeGameObject:
                    GameObject root = targetRoot != null ? targetRoot : gameObject;
                    if (root != null) root.SetActive(false);
                    break;

                case GateMode.DeactivateContent:
                    for (int i = 0; i < controllersToDisable.Length; i++)
                        if (controllersToDisable[i] != null) controllersToDisable[i].enabled = false;
                    for (int i = 0; i < contentToHide.Length; i++)
                        if (contentToHide[i] != null) contentToHide[i].SetActive(false);
                    break;
            }

            if (logOnGate)
                Debug.Log($"[SceneEntryGate] {name}: CLOSED (entry={StoryState.Instance.EntryPoint}, own={ownEntry}, mode={mode})");
        }
    }
}
