using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MainKarinding.Cutscenes;

/// <summary>
/// Bark Abah Scene 6 di dalam MinigameCrafting (non-blocking, one-time).
/// - Encourage ("Nya kitu, Ba...") saat phase -> Tapping (50%).
/// - Koreksi ("Sanes didinya, Ba...") saat OnFirstWrong (cut-invalid pertama / fall pertama).
/// Sprite diasumsikan TANPA bubble. Teks hanya 1 bahasa: Sunda.
/// </summary>
[AddComponentMenu("Main Karinding/Crafting/Scene6 Abah Bark")]
public class Scene6AbahBark : MonoBehaviour
{
    [Header("Refs (auto-find kalau kosong)")]
    [SerializeField] private CraftingAssemblyController controller;
    [SerializeField] private AbahSpriteSet abahSpriteSet;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI sundaText;
    [SerializeField] private TextMeshProUGUI indoText;
    [SerializeField] private CanvasGroup barkGroup;

    [Header("Bark - Encourage (lancar)")]
    [TextArea] [SerializeField] private string encourageSunda = "Nya kitu, Ba. Titen jeung teu gagabah, eta nu disebat cermat teh.";
    [TextArea] [SerializeField] private string encourageIndo = "Iya kaya gitu, Ba. Teliti dan nggak gegabah, itu yang disebut cermat.";

    [Header("Bark - Koreksi (salah pertama)")]
    [TextArea] [SerializeField] private string koreksiSunda = "Sanes didinya, Ba. Titenan deui bentuk na, meh pas.";
    [TextArea] [SerializeField] private string koreksiIndo = "Bukan di situ, Ba. Perhatiin lagi bentuknya, biar pas.";

    [Header("Settings")]
    [SerializeField] private float barkSeconds = 2.8f;
    [SerializeField] private float fadeSeconds = 0.3f;

    private bool encourageShown;
    private bool koreksiShown;
    private Coroutine running;

    private void Awake()
    {
        if (controller == null) controller = Object.FindFirstObjectByType<CraftingAssemblyController>();
        if (barkGroup == null) barkGroup = GetComponent<CanvasGroup>();
        if (barkGroup == null) barkGroup = GetComponentInChildren<CanvasGroup>(true);
        if (portraitImage == null) portraitImage = GetComponentInChildren<Image>(true);
        if (barkGroup != null)
        {
            barkGroup.alpha = 0f;
            barkGroup.blocksRaycasts = false;
            barkGroup.interactable = false;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        // 1 bahasa (Sunda): GO Indo dinonaktifkan permanen.
        if (indoText != null) indoText.gameObject.SetActive(false);
        HideImmediate();
    }

    private void OnEnable()
    {
        if (controller == null) controller = Object.FindFirstObjectByType<CraftingAssemblyController>();
        if (controller != null)
        {
            controller.OnPhaseChanged -= OnPhase;
            controller.OnPhaseChanged += OnPhase;
            controller.OnFirstWrong -= OnWrong;
            controller.OnFirstWrong += OnWrong;
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnPhaseChanged -= OnPhase;
            controller.OnFirstWrong -= OnWrong;
        }
    }

    private void OnPhase(AssemblyPhase phase)
    {
        if (phase != AssemblyPhase.Tapping || encourageShown) return;
        encourageShown = true;
        Sprite s = abahSpriteSet != null ? abahSpriteSet.encourage : null;
        ShowBark(s, encourageSunda, encourageIndo);
    }

    private void OnWrong()
    {
        if (koreksiShown) return;
        koreksiShown = true;
        Sprite s = abahSpriteSet != null ? abahSpriteSet.koreksi : null;
        ShowBark(s, koreksiSunda, koreksiIndo);
    }

    public void ShowBark(Sprite portrait, string sunda, string indo)
    {
        if (portraitImage != null && portrait != null) portraitImage.sprite = portrait;
        if (sundaText != null) sundaText.text = sunda;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(BarkRoutine());
    }

    public void HideImmediate()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        if (barkGroup != null) barkGroup.alpha = 0f;
    }

    private IEnumerator BarkRoutine()
    {
        yield return FadeTo(1f);
        yield return new WaitForSeconds(Mathf.Max(0.5f, barkSeconds));
        yield return FadeTo(0f);
        running = null;
    }

    private IEnumerator FadeTo(float target)
    {
        if (barkGroup == null) yield break;
        float start = barkGroup.alpha;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            barkGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds)));
            yield return null;
        }
        barkGroup.alpha = target;
    }
}
