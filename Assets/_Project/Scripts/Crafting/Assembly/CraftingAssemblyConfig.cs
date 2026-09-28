using UnityEngine;

[CreateAssetMenu(fileName = "CraftingAssemblyConfig", menuName = "Karinding/CraftingAssemblyConfig")]
public class CraftingAssemblyConfig : ScriptableObject
{
    [Header("Cut Phase (0% -> 50%)")]
    [Tooltip("Progress ditambah tiap cut valid. Default 10 = 5 cut valid sampai 50%.")]
    public float cutIncrement = 10f;

    [Tooltip("Jarak minimum swipe dalam pixel screen untuk dihitung sebagai cut. ~30-40% lebar area gesture.")]
    public float cutMinDistance = 150f;

    [Tooltip("Rasio kelurusan minimum (displacement / pathLength). 1.0 = garis sempurna, 0.85 = toleran untuk jari manusia.")]
    [Range(0f, 1f)]
    public float cutMinStraightness = 0.85f;

    [Tooltip("Jika true, cutMinDistance diinterpretasi sebagai fraksi lebar RectTransform (0-1) bukan pixel absolut. Untuk device independence.")]
    public bool useNormalizedCutDistance = false;

    [Tooltip("Jika useNormalizedCutDistance true, nilai ini dipakai (0.3 = 30% lebar area).")]
    [Range(0f, 1f)]
    public float cutMinDistanceNormalized = 0.32f;

    [Tooltip("Reward bonus progress jika straightness sangat tinggi (>0.95). Opsional, 0 = tidak ada bonus.")]
    public float cutStraightnessBonus = 0f;

    [Tooltip("Jika true, swipe hanya valid dari atas ke bawah (arah golok jatuh).")]
    public bool cutRequireTopToBottom = true;

    [Tooltip("Rasio dominan vertikal: |dy| harus >= |dx| * nilai ini. 1.25 = lebih vertikal daripada horizontal.")]
    public float cutMinVerticalRatio = 1.25f;

    [Tooltip("Threshold dir.y negatif untuk dianggap kebawah. -0.35 = toleransi diagonal bawah, -0.8 = harus hampir vertikal.")]
    public float cutMinDownDot = -0.35f;

    [Header("Tap Phase (50% -> 100%)")]
    [Tooltip("Progress ditambah tiap tap valid. 2.5 = 20 tap (mode susah).")]
    public float tapIncrement = 2.5f;

    [Tooltip("Kecepatan decay dalam % per detik saat idle melebihi grace time.")]
    public float tapDecayRate = 15f;

    [Tooltip("Grace window dalam detik sebelum decay mulai. 0.25 = harus ~4 tap/detik.")]
    public float tapDecayGraceTime = 0.25f;

    [Tooltip("Interval minimum antar tap dalam detik untuk anti-macro. 0.05 = max ~20 tap/detik.")]
    public float tapMinInterval = 0.05f;

    [Header("Phase Thresholds")]
    [Tooltip("Threshold progress untuk transisi Cutting -> Tapping. Default 50.")]
    public float cuttingToTappingThreshold = 50f;

    [Tooltip("Threshold progress untuk Completed. Default 100.")]
    public float completionThreshold = 100f;

    [Header("Tap Fail")]
    [Tooltip("Berapa kali balik ke 50% (fall) sebelum dianggap gagal dan ulang swipe. Default 2.")]
    public int maxTapFallsBeforeFail = 2;
}
