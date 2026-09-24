using UnityEngine;
using System.Collections;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Handle sprite swap animation for character (e.g., Beat 6: Rimba menoleh).
    /// Uses coroutine-based frame sequencing with configurable duration.
    /// </summary>
    public class CutsceneSpriteSwapper : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer targetRenderer;

        [Header("Animation Settings")]
        [SerializeField] private float frameDuration = 0.12f;

        private Coroutine swapCoroutine;

    /// <summary>
    /// Play sprite swap sequence.
    /// </summary>
    /// <param name="sprites">Array of sprites to swap through in order</param>
    /// <param name="frameDurations">Optional array of per-frame durations. If null, uses default frameDuration</param>
    /// <param name="onComplete">Callback when sequence completes</param>
    public void PlaySwapSequence(Sprite[] sprites, float[] frameDurations = null, System.Action onComplete = null)
    {
        if (targetRenderer == null)
        {
            Debug.LogError("CutsceneSpriteSwapper: targetRenderer is null!");
            onComplete?.Invoke();
            return;
        }

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("CutsceneSpriteSwapper: sprites array is null or empty!");
            onComplete?.Invoke();
            return;
        }

        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);

        swapCoroutine = StartCoroutine(SwapSequenceCoroutine(sprites, frameDurations, onComplete));
    }

        /// <summary>
        /// Stop current swap sequence.
        /// </summary>
        public void Stop()
        {
            if (swapCoroutine != null)
            {
                StopCoroutine(swapCoroutine);
                swapCoroutine = null;
            }
        }

    private IEnumerator SwapSequenceCoroutine(Sprite[] sprites, float[] frameDurations, System.Action onComplete)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && targetRenderer != null)
                targetRenderer.sprite = sprites[i];

            // Use per-frame duration if provided, otherwise use default
            float duration = (frameDurations != null && i < frameDurations.Length) 
                ? frameDurations[i] 
                : frameDuration;

            yield return new WaitForSeconds(duration);
        }

        onComplete?.Invoke();
        swapCoroutine = null;
    }
    }
}
