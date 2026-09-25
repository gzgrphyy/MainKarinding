using UnityEngine;
using UnityEngine.UI;

public class DebugRhythmTester : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private RhythmChartConfig testConfig;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        if (RhythmMinigameController.Instance == null)
        {
            Debug.LogError("[DebugRhythmTester] RhythmMinigameController instance not found in scene.");
            return;
        }

        if (testConfig == null)
        {
            Debug.LogError("[DebugRhythmTester] testConfig not assigned.");
            return;
        }

        RhythmMinigameController.Instance.StartMinigame(testConfig, true);
        if (startButton != null) startButton.gameObject.SetActive(false);
        Debug.Log("[DebugRhythmTester] StartMinigame called with config: " + testConfig.name);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartButtonClicked);
    }
}
