using System;
using System.Collections;
using UnityEngine;

namespace Karinding.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private const string TranslationPrefKey = "dialogue_show_translation";

        [SerializeField] private DialogueView view;
        [SerializeField] private string playerSpeakerName = "Rimba";
        [Tooltip("Tampilkan pilihan yang diambil player sebagai baris ucapan Rimba sebelum jawaban NPC")]
        [SerializeField] private bool echoChosenChoice = true;
        [SerializeField, Min(1f)] private float charsPerSecond = 40f;

        public bool IsPlaying { get; private set; }
        public bool ShowTranslation => PlayerPrefs.GetInt(TranslationPrefKey, 1) == 1;

        public event Action<string> OnDialogueStarted;          // sequenceId
        public event Action<string> OnDialogueEnded;            // sequenceId
        public event Action<string> OnDialogueEvent;            // eventId dari LineData.eventId
        public event Action<string, int> OnAffinityChanged;     // npcId, delta
        public event Action<LineData> OnLineShown;              // dipicu setiap baris tampil (titik sambung FMOD/animasi)

        private Coroutine _routine;
        private LineData _currentLine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTranslation(bool on)
        {
            PlayerPrefs.SetInt(TranslationPrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Set preferensi terjemahan sekaligus refresh baris yang sedang tampil.
        /// Dipanggil oleh toggle di DialogueView agar perubahan terlihat langsung.
        /// </summary>
        public void SetTranslationAndRefresh(bool on)
        {
            SetTranslation(on);
            RefreshCurrentLine();
        }

        /// <summary>Refresh tampilan terjemahan untuk baris yang sedang aktif (tanpa mengulang efek ketik).</summary>
        public void RefreshCurrentLine()
        {
            if (!IsPlaying || _currentLine == null || view == null) return;
            view.RefreshTranslation(ShowTranslation, _currentLine);
            view.RefreshChoiceTranslations(ShowTranslation);
        }

        public LineData CurrentLine => _currentLine;

        public void StartDialogue(DialogueSequence sequence, Action onComplete = null)
        {
            if (sequence == null) { Debug.LogWarning("[Dialogue] Sequence null."); return; }
            if (IsPlaying) { Debug.LogWarning("[Dialogue] Masih ada dialog yang berjalan."); return; }
            _routine = StartCoroutine(RunSequence(sequence, onComplete));
        }

        public void StopDialogue()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            IsPlaying = false;
            view.HideImmediate();
        }

        private IEnumerator RunSequence(DialogueSequence sequence, Action onComplete)
        {
            IsPlaying = true;
            OnDialogueStarted?.Invoke(sequence.sequenceId);
            yield return view.Show();

            foreach (DialogueLine line in sequence.lines)
            {
                bool hasChoices = line.choices != null && line.choices.Count > 0;

                // baris yang punya pilihan: tidak menunggu tap, langsung munculkan pilihan
                yield return PlayLine(line, waitForTap: !hasChoices);
                if (!hasChoices) continue;

                int picked = -1;
                view.ShowChoices(line.choices, ShowTranslation, i => { if (picked < 0) picked = i; });
                while (picked < 0) yield return null;
                view.HideChoices();

                DialogueChoice choice = line.choices[picked];
                if (choice.affinityDelta != 0 && !string.IsNullOrEmpty(choice.npcId))
                    OnAffinityChanged?.Invoke(choice.npcId, choice.affinityDelta);

                if (echoChosenChoice)
                {
                    yield return PlayLine(new LineData
                    {
                        speakerName = playerSpeakerName,
                        portraitSide = DialogueSide.Left,
                        text = choice.label,
                        translation = choice.labelTranslation
                    }, true);
                }

                foreach (LineData response in choice.responseLines)
                    yield return PlayLine(response, true);
            }

            yield return view.Hide();
            _currentLine = null;
            IsPlaying = false;
            _routine = null;
            OnDialogueEnded?.Invoke(sequence.sequenceId);
            onComplete?.Invoke();
        }

        private IEnumerator PlayLine(LineData line, bool waitForTap)
        {
            _currentLine = line;
            if (!string.IsNullOrEmpty(line.eventId)) OnDialogueEvent?.Invoke(line.eventId);

            view.SetLine(line, ShowTranslation);
            OnLineShown?.Invoke(line);
            yield return view.TypeText(line.text, charsPerSecond);

            if (!waitForTap) yield break;

            view.SetContinueIndicator(true);
            yield return view.WaitForTap();
            view.SetContinueIndicator(false);
        }
    }
}