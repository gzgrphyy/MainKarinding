using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Karinding.Dialogue
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text translationText; // opsional

        private string _labelTranslation;

        public void Setup(string label, string translation, Action onClick)
        {
            _labelTranslation = translation;
            labelText.text = label;

            if (translationText != null)
            {
                bool has = !string.IsNullOrEmpty(translation);
                translationText.gameObject.SetActive(has);
                translationText.text = translation;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        /// <summary>Refresh visibilitas terjemahan tanpa mengubah logic pilihan.</summary>
        public void RefreshTranslation(bool showTranslation)
        {
            if (translationText == null) return;
            bool has = showTranslation && !string.IsNullOrEmpty(_labelTranslation);
            translationText.gameObject.SetActive(has);
            if (has) translationText.text = _labelTranslation;
        }
    }
}