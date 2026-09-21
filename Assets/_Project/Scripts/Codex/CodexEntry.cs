using UnityEngine;

namespace Karinding.Codex
{
    [CreateAssetMenu(fileName = "NewCodexEntry", menuName = "Karinding/Codex Entry")]
    public class CodexEntry : ScriptableObject
    {
        public string entryId;
        public string title;
        [TextArea(3, 6)] public string content;
        [Tooltip("Unlocked di save; runtime CodexManager yang atur. Default false.")]
        public bool unlocked;
    }
}
