using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karinding.Dialogue
{
    public enum DialogueSide { Left, Right }

    [Serializable]
    public class LineData
    {
        public string speakerName = "Abah";
        public bool isNarration;
        public Sprite portrait;
        public DialogueSide portraitSide = DialogueSide.Right;
        [TextArea(2, 6)] public string text;          // teks Sunda (utama)
        [TextArea(2, 6)] public string translation;   // subtitle Indonesia (opsional)
        public string eventId;                        // dipicu saat baris ini tampil (mis. CODEX_PANCEPENGAN)
    }

    [Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 3)] public string label;            // ucapan Rimba
        [TextArea(1, 3)] public string labelTranslation;
        public string npcId;
        public int affinityDelta;
        public List<LineData> responseLines = new List<LineData>();
    }

    [Serializable]
    public class DialogueLine : LineData
    {
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Karinding/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string sequenceId;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}