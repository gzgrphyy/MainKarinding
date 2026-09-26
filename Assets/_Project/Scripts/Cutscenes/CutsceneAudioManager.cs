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

        private Coroutine duckCoroutine;

        /// <summary>
        /// Duck ambient sesaat (pengganti AudioMixer snapshot Dropout — project tidak pakai mixer).
        /// Turun cepat ke dipVolume, tahan, lalu kembali pelan ke volume semula. Aman dipanggil saat fade lain jalan.
        /// </summary>
        public void DuckAmbient(float dipVolume = 0.08f, float dipSeconds = 0.08f, float holdSeconds = 0.4f, float returnSeconds = 1.2f)
        {
            if (ambientSource == null || !ambientSource.isPlaying) return;
            if (duckCoroutine != null) StopCoroutine(duckCoroutine);
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
            duckCoroutine = StartCoroutine(DuckCoroutine(ambientSource.volume, dipVolume, dipSeconds, holdSeconds, returnSeconds));
        }

        private IEnumerator DuckCoroutine(float startVolume, float dipVolume, float dipSeconds, float holdSeconds, float returnSeconds)
        {
            float t = 0f;
            while (t < dipSeconds)
            {
                t += Time.deltaTime;
                if (ambientSource == null) break;
                ambientSource.volume = Mathf.Lerp(startVolume, dipVolume, Mathf.Clamp01(t / Mathf.Max(0.01f, dipSeconds)));
                yield return null;
            }
            if (ambientSource != null) ambientSource.volume = dipVolume;
            yield return new WaitForSeconds(holdSeconds);
            t = 0f;
            while (t < returnSeconds)
            {
                t += Time.deltaTime;
                if (ambientSource == null) break;
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, returnSeconds));
                ambientSource.volume = Mathf.Lerp(dipVolume, startVolume, fadeCurve.Evaluate(k));
                yield return null;
            }
            if (ambientSource != null) ambientSource.volume = startVolume;
            duckCoroutine = null;
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
