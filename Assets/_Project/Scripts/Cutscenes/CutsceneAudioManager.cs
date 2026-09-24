using UnityEngine;
using System.Collections;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Manage audio for cutscenes: ambient loops, SFX, fade in/out.
    /// </summary>
    public class CutsceneAudioManager : MonoBehaviour
    {
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Fade Settings")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine fadeCoroutine;

        /// <summary>
        /// Play ambient loop audio.
        /// </summary>
        public void PlayAmbient(AudioClip clip, float volume = 1f)
        {
            if (ambientSource == null || clip == null) return;

            ambientSource.clip = clip;
            ambientSource.volume = volume;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        /// <summary>
        /// Stop ambient audio immediately.
        /// </summary>
        public void StopAmbient()
        {
            if (ambientSource == null) return;
            ambientSource.Stop();
        }

        /// <summary>
        /// Fade out ambient audio over specified duration.
        /// </summary>
        public void FadeOutAmbient(float duration = -1f)
        {
            if (ambientSource == null) return;

            if (duration < 0)
                duration = fadeOutDuration;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeOutCoroutine(ambientSource, duration));
        }

        /// <summary>
        /// Play one-shot SFX.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

    private IEnumerator FadeOutCoroutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Apply curve for smooth fade
            float curveValue = fadeCurve.Evaluate(t);
            source.volume = Mathf.Lerp(startVolume, 0f, curveValue);
            
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        fadeCoroutine = null;
    }
    }
}
