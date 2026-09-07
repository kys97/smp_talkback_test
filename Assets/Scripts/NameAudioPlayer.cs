using System.Collections;
using UnityEngine;

namespace NamnyeoChilse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class NameAudioPlayer : MonoBehaviour
    {
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            // This game has no audio pause mode; listener pause must not look like completion.
            source.ignoreListenerPause = true;
        }

        public IEnumerator PlayAndWait(AudioClip clip, float speed = 1f)
        {
            Stop();
            source.pitch = float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0 ? 1f : Mathf.Clamp(speed, 0.1f, 3f);
            if (clip == null) yield break;

            if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
            {
                Debug.LogWarning($"이름 음성 '{clip.name}' 로딩 실패: 음성 없이 입력을 허용합니다.", this);
                yield break;
            }
            while (clip.loadState == AudioDataLoadState.Loading) yield return null;
            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"이름 음성 '{clip.name}' 로딩 실패: 음성 없이 입력을 허용합니다.", this);
                yield break;
            }

            source.clip = clip;
            source.Play();
            // Wait for the audio engine, not clip.length or a fixed delay.
            yield return null;
            while (source.isPlaying) yield return null;
            source.clip = null;
            source.pitch = 1f;
        }

        public void Stop()
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.pitch = 1f;
        }

        private void OnDisable() => Stop();
    }
}
