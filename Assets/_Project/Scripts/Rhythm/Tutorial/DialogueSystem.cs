using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float typewriterSpeed = 0.040f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = Color.white;

    public event Action OnDialogueComplete;

    private Canvas _canvas;
    private CanvasGroup _panelGroup;
    private TextMeshProUGUI _textComponent;
    private TextMeshProUGUI _continueIndicator;
    private GameObject _panelObject;
    private bool _isShowing;
    private bool _typewriterDone;
    private bool _dismissRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[DialogueSystem] Canvas not found, BuildUI will fail");
            return;
        }
        Debug.Log($"[DialogueSystem] Awake Instance set, canvas={_canvas.name}");
        BuildUI();
    }

    private void BuildUI()
    {
        // Panel container
        _panelObject = new GameObject("DialoguePanel");
        _panelObject.transform.SetParent(_canvas.transform, false);

        RectTransform panelRect = _panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 20f);
        panelRect.sizeDelta = new Vector2(-40f, 150f);

        Image panelImage = _panelObject.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = true;

        _panelGroup = _panelObject.AddComponent<CanvasGroup>();
        _panelGroup.alpha = 0f;
        _panelGroup.blocksRaycasts = false;
        _panelGroup.interactable = false;

        // Tap catcher (on top of panel)
        GameObject tapCatcher = new GameObject("TapCatcher");
        tapCatcher.transform.SetParent(_panelObject.transform, false);

        RectTransform tapRect = tapCatcher.AddComponent<RectTransform>();
        tapRect.anchorMin = Vector2.zero;
        tapRect.anchorMax = Vector2.one;
        tapRect.sizeDelta = Vector2.zero;

        Image tapImage = tapCatcher.AddComponent<Image>();
        tapImage.color = Color.clear;
        tapImage.raycastTarget = true;

        EventTrigger trigger = tapCatcher.AddComponent<EventTrigger>();
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((_) => OnTap());
        trigger.triggers.Add(pointerDown);

        // Dialogue text
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(_panelObject.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        _textComponent = textObj.AddComponent<TextMeshProUGUI>();
        _textComponent.fontSize = 24;
        _textComponent.color = textColor;
        _textComponent.alignment = TextAlignmentOptions.Center;
        _textComponent.enableWordWrapping = true;
        _textComponent.overflowMode = TextOverflowModes.Ellipsis;

        // Continue indicator (bottom right)
        GameObject indicatorObj = new GameObject("ContinueIndicator");
        indicatorObj.transform.SetParent(_panelObject.transform, false);

        RectTransform indicatorRect = indicatorObj.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0f);
        indicatorRect.pivot = new Vector2(0.5f, 0f);
        indicatorRect.anchoredPosition = new Vector2(0f, 12f);
        indicatorRect.sizeDelta = new Vector2(250f, 55f);

        _continueIndicator = indicatorObj.AddComponent<TextMeshProUGUI>();
        _continueIndicator.text = "Tap ▼";
        _continueIndicator.fontSize = 26;
        _continueIndicator.color = new Color(1f, 1f, 1f, 0.9f);
        _continueIndicator.alignment = TextAlignmentOptions.Center;

        EventTrigger indicatorTrigger = indicatorObj.AddComponent<EventTrigger>();
        EventTrigger.Entry indicatorPointerDown = new EventTrigger.Entry();
        indicatorPointerDown.eventID = EventTriggerType.PointerDown;
        indicatorPointerDown.callback.AddListener((_) => OnTap());
        indicatorTrigger.triggers.Add(indicatorPointerDown);

        _continueIndicator.gameObject.SetActive(false);
    }

    public void ShowDialogue(string text)
    {
        Debug.Log($"[DialogueSystem] ShowDialogue called: '{text}' isShowing={_isShowing} panel={(_panelObject!=null?_panelObject.name:"null")}");
        if (_isShowing) return;
        if (_panelObject == null)
        {
            Debug.LogError("[DialogueSystem] _panelObject null, rebuilding");
            BuildUI();
        }
        StartCoroutine(ShowDialogueRoutine(text));
    }

    private IEnumerator ShowDialogueRoutine(string text)
    {
        _isShowing = true;
        _typewriterDone = false;
        _dismissRequested = false;

        _textComponent.text = "";
        _continueIndicator.gameObject.SetActive(false);
        _panelGroup.alpha = 0f;
        _panelGroup.blocksRaycasts = true;
        _panelGroup.interactable = true;
        _panelObject.SetActive(true);

        // Fade in
        _panelGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(fadeInDuration);

        // Typewriter
        for (int i = 0; i <= text.Length; i++)
        {
            if (_dismissRequested)
            {
                _textComponent.text = text;
                break;
            }

            _textComponent.text = text.Substring(0, i);
            yield return new WaitForSeconds(typewriterSpeed);
        }

        _textComponent.text = text;
        _typewriterDone = true;

        // Show continue indicator
        ShowContinueIndicator();

        // Wait for tap to dismiss
        yield return new WaitUntil(() => _dismissRequested);

        // Fade out
        _panelGroup.DOKill();
        _panelGroup.blocksRaycasts = false;
        _panelGroup.interactable = false;

        _panelGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(fadeOutDuration);

        _panelObject.SetActive(false);
        _isShowing = false;

        OnDialogueComplete?.Invoke();
    }

    private void ShowContinueIndicator()
    {
        _continueIndicator.gameObject.SetActive(true);
        _continueIndicator.DOKill();
        _continueIndicator.alpha = 0.4f;

        // Blink loop: fade between 0.4 and 1
        _continueIndicator.DOFade(1f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnTap()
    {
        if (!_isShowing) return;
        _dismissRequested = true;
    }

    public void ForceDismiss()
    {
        StopAllCoroutines();
        _panelGroup.DOKill();
        if (_continueIndicator != null) _continueIndicator.DOKill();
        _panelGroup.alpha = 0f;
        _panelGroup.blocksRaycasts = false;
        _panelGroup.interactable = false;
        _panelObject.SetActive(false);
        _isShowing = false;
    }
}
