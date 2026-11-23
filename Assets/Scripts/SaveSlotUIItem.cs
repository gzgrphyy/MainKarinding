using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KarindingLegacy.Save;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Komponen UI untuk merepresentasikan satu kartu Slot Penyimpanan pada modal Save/Load.
    /// </summary>
    public class SaveSlotUIItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private int slotIndex = 1;
        [SerializeField] private TMP_Text slotTitleText;
        [SerializeField] private TMP_Text slotDetailsText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private CanvasGroup canvasGroup;

        public int SlotIndex => slotIndex;

        public void Initialize(int index)
        {
            slotIndex = index;
            if (slotTitleText != null) slotTitleText.text = LocalizationManager.Format("slot.title", index);
        }

        public void RefreshDisplay(bool isNewGameMode, Action<int> onSelectCallback, Action<int> onDeleteCallback = null)
        {
            SaveSlotData data = SaveSystem.GetSlot(slotIndex);

            if (slotTitleText != null)
            {
                slotTitleText.text = LocalizationManager.Format("slot.title", slotIndex);
            }

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (isNewGameMode)
            {
                if (data.hasData)
                {
                    if (slotDetailsText != null)
                    {
                        if (data.isCorrupt)
                        {
                            slotDetailsText.text = LocalizationManager.Get("slot.corruptNew");
                            slotDetailsText.color = new Color(0.95f, 0.45f, 0.35f);
                        }
                        else
                        {
                            slotDetailsText.text = LocalizationManager.Format("slot.filledNew", data.chapterName, data.level);
                            slotDetailsText.color = new Color(0.96f, 0.72f, 0.35f);
                        }
                    }

                    if (selectButton != null)
                    {
                        selectButton.interactable = false;
                        selectButton.onClick.RemoveAllListeners();
                    }

                    if (canvasGroup != null) canvasGroup.alpha = 0.6f;
                }
                else
                {
                    if (slotDetailsText != null)
                    {
                        slotDetailsText.text = LocalizationManager.Get("slot.emptyNew");
                        slotDetailsText.color = new Color(0.85f, 0.82f, 0.75f);
                    }

                    if (selectButton != null)
                    {
                        selectButton.interactable = true;
                        selectButton.onClick.RemoveAllListeners();
                        selectButton.onClick.AddListener(() => onSelectCallback?.Invoke(slotIndex));
                    }

                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }
            }
            else
            {
                if (data.isCorrupt)
                {
                    if (slotDetailsText != null)
                    {
                        slotDetailsText.text = LocalizationManager.Get("slot.corruptLoad");
                        slotDetailsText.color = new Color(0.95f, 0.45f, 0.35f);
                    }

                    if (selectButton != null)
                    {
                        selectButton.interactable = false;
                        selectButton.onClick.RemoveAllListeners();
                    }

                    if (canvasGroup != null) canvasGroup.alpha = 0.55f;
                }
                else if (data.hasData)
                {
                    if (slotDetailsText != null)
                    {
                        slotDetailsText.text = LocalizationManager.Format("slot.filledLoad", data.chapterName, data.level, data.saveDate);
                        slotDetailsText.color = new Color(0.96f, 0.88f, 0.65f);
                    }

                    if (selectButton != null)
                    {
                        selectButton.interactable = true;
                        selectButton.onClick.RemoveAllListeners();
                        selectButton.onClick.AddListener(() => onSelectCallback?.Invoke(slotIndex));
                    }

                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }
                else
                {
                    if (slotDetailsText != null)
                    {
                        slotDetailsText.text = LocalizationManager.Get("slot.empty");
                        slotDetailsText.color = new Color(0.60f, 0.58f, 0.55f);
                    }

                    if (selectButton != null)
                    {
                        selectButton.interactable = false;
                        selectButton.onClick.RemoveAllListeners();
                    }

                    if (canvasGroup != null) canvasGroup.alpha = 0.55f;
                }
            }

            if (deleteButton != null)
            {
                bool showDelete = data.hasData;
                deleteButton.gameObject.SetActive(showDelete);
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => onDeleteCallback?.Invoke(slotIndex));
            }
        }
    }
}
