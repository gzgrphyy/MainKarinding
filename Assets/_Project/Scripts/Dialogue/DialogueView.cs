using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Karinding.Dialogue
{
    // Taruh di DialogueRoot (full-screen, punya Image alpha 0 + Raycast Target ON)
    public class DialogueView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Panel")]
        [SerializeField] private GameObject nameTag;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text translationText;
        [SerializeField] private RectTransform continueIndicator;

        [Header("Portraits")]
        [SerializeField] private Image portraitLeft;
        [SerializeField] private Image portraitRight;

        [Header("Choices")]
        [SerializeField] private RectTransform choiceContainer;
        [SerializeField] private DialogueChoiceButton choiceButtonPrefab;

        private bool _tapped;
        private Coroutine _indicatorRoutine;
        private float _indicatorBaseY;
        private Coroutine _fadeRoutine;
        private readonly List<GameObject> _spawnedChoices = new List<GameObject>();
        private LineData _currentLine;
        private List<DialogueChoice> _currentChoices;

        private void Awake()
        {
            HideImmediate();
            if (continueIndicator != null)
            {
                _indicatorBaseY = continueIndicator.anchoredPosition.y;
                _indicatorRoutine = StartCoroutine(BounceIndicator());
            }
        }

        private void OnDestroy()
        {
            if (_indicatorRoutine != null) StopCoroutine(_indicatorRoutine);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        }

        public void OnPointerClick(PointerEventData eventData) => _tapped = true;

        // ---------- Show / Hide ----------
        public IEnumerator Show()
        {
            ClearContent();
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
            yield return FadeCanvasGroup(rootGroup.alpha, 1f);
        }

        public IEnumerator Hide()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            yield return FadeCanvasGroup(rootGroup.alpha, 0f);
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
            ClearContent();
        }

        public void HideImmediate()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
            HideChoices();
            ClearContent();
        }

        private IEnumerator FadeCanvasGroup(float from, float to)
        {
            // Manual fade tanpa DOTween — agar tidak depend DG.Tweening
            // DOTween bisa di-enable lagi nanti: ganti dengan rootGroup.DOFade(to, fadeDuration)
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rootGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            rootGroup.alpha = to;
        }

        private IEnumerator BounceIndicator()
        {
            // Pengganti DOAnchorPosY yoyo — tanpa DOTween
            // Gerak naik-turun 8px, 0.5s, ease InOutSine
            if (continueIndicator == null) yield break;
            while (true)
            {
                // Hanya anim saat indicator aktif, tapi coroutine tetap jalan (murah)
                if (continueIndicator.gameObject.activeSelf)
                {
                    float t = 0f;
                    float duration = 0.5f;
                    Vector2 start = new Vector2(continueIndicator.anchoredPosition.x, _indicatorBaseY);
                    Vector2 end = new Vector2(start.x, _indicatorBaseY - 8f);
                    // turun
                    while (t < 1f)
                    {
                        if (continueIndicator == null) yield break;
                        t += Time.unscaledDeltaTime / duration;
                        float eased = 0.5f * (1f - Mathf.Cos(Mathf.PI * Mathf.Clamp01(t))); // InOutSine
                        continueIndicator.anchoredPosition = Vector2.Lerp(start, end, eased);
                        yield return null;
                    }
                    t = 0f;
                    while (t < 1f)
                    {
                        if (continueIndicator == null) yield break;
                        t += Time.unscaledDeltaTime / duration;
                        float eased = 0.5f * (1f - Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)));
                        continueIndicator.anchoredPosition = Vector2.Lerp(end, start, eased);
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void ClearContent()
        {
            bodyText.text = "";
            translationText.text = "";
            translationText.gameObject.SetActive(false);
            portraitLeft.gameObject.SetActive(false);
            portraitRight.gameObject.SetActive(false);
            if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
        }

        // ---------- Line ----------
        public void SetLine(LineData line, bool showTranslation)
        {
            _currentLine = line;
            bool narration = line.isNarration || string.IsNullOrEmpty(line.speakerName);
            nameTag.SetActive(!narration);
            speakerText.text = line.speakerName;

            bool hasTranslation = showTranslation && !string.IsNullOrEmpty(line.translation);
            translationText.gameObject.SetActive(hasTranslation);
            translationText.text = line.translation;

            bool hasPortrait = line.portrait != null;
            bool left = line.portraitSide == DialogueSide.Left;
            portraitLeft.gameObject.SetActive(hasPortrait && left);
            portraitRight.gameObject.SetActive(hasPortrait && !left);
            if (hasPortrait)
            {
                Image target = left ? portraitLeft : portraitRight;
                target.sprite = line.portrait;
                target.preserveAspect = true;
            }

            if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
        }

        /// <summary>Refresh terjemahan baris aktif tanpa mengulang efek ketik.</summary>
        public void RefreshTranslation(bool showTranslation, LineData line = null)
        {
            if (line != null) _currentLine = line;
            if (_currentLine == null) return;
            bool hasTranslation = showTranslation && !string.IsNullOrEmpty(_currentLine.translation);
            translationText.gameObject.SetActive(hasTranslation);
            translationText.text = _currentLine.translation;
        }

        /// <summary>Refresh terjemahan pada tombol pilihan yang sedang tampil.</summary>
        public void RefreshChoiceTranslations(bool showTranslation)
        {
            if (_currentChoices == null || _spawnedChoices.Count == 0) return;
            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] == null) continue;
                var btn = _spawnedChoices[i].GetComponent<DialogueChoiceButton>();
                if (btn != null) btn.RefreshTranslation(showTranslation);
            }
        }

        public IEnumerator TypeText(string text, float charsPerSecond)
        {
            bodyText.text = text ?? "";
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;

            _tapped = false;
            float elapsed = 0f;
            int shown = 0;
            while (shown < total)
            {
                if (_tapped) { _tapped = false; break; } // tap saat mengetik = tampilkan semua
                elapsed += Time.unscaledDeltaTime * charsPerSecond;
                shown = Mathf.Min(total, Mathf.FloorToInt(elapsed));
                bodyText.maxVisibleCharacters = shown;
                yield return null;
            }
            bodyText.maxVisibleCharacters = total;
        }

        public IEnumerator WaitForTap()
        {
            _tapped = false;
            while (!_tapped) yield return null;
            _tapped = false;
        }

        public void SetContinueIndicator(bool on)
        {
            if (continueIndicator != null) continueIndicator.gameObject.SetActive(on);
        }

        // ---------- Choices ----------
        public void ShowChoices(List<DialogueChoice> choices, bool showTranslation, Action<int> onPick)
        {
            HideChoices();
            _currentChoices = choices;
            choiceContainer.gameObject.SetActive(true);

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                DialogueChoiceButton btn = Instantiate(choiceButtonPrefab, choiceContainer);
                btn.Setup(choices[i].label,
                           showTranslation ? choices[i].labelTranslation : null,
                           () => onPick(index));

                // Anim scale tanpa DOTween — fallback manual
                // Jika DOTween tersedia, bisa ganti dengan: btn.transform.DOScale(1,0.2f).SetDelay(0.06f*i).SetEase(Ease.OutBack)
                btn.transform.localScale = Vector3.zero;
                StartCoroutine(ScaleUp(btn.transform, 0.06f * i));

                _spawnedChoices.Add(btn.gameObject);
            }
        }

        private IEnumerator ScaleUp(Transform t, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (t == null) yield break;
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (t == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                // Approx OutBack: overshoot 1.70158
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(p - 1f, 3) + c1 * Mathf.Pow(p - 1f, 2);
                float s = Mathf.Lerp(0f, 1f, eased);
                t.localScale = new Vector3(s, s, s);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        public void HideChoices()
        {
            foreach (GameObject go in _spawnedChoices)
                if (go != null) Destroy(go);
            _spawnedChoices.Clear();
            _currentChoices = null;
            if (choiceContainer != null) choiceContainer.gameObject.SetActive(false);
        }
    }
}
