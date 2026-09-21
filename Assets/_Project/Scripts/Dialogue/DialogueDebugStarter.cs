using UnityEngine;

namespace Karinding.Dialogue
{
    public class DialogueDebugStarter : MonoBehaviour
    {
        [SerializeField] private DialogueSequence sequence;

        // Hubungkan ke OnClick sebuah Button
        public void Play() => DialogueManager.Instance.StartDialogue(sequence, () => Debug.Log("[Dialogue] selesai"));
    }
}