using System.Collections;
using UnityEngine;

namespace NamnyeoChilse
{
    // Separate source using the answered question's speed, not the next question's tier.
    [DisallowMultipleComponent]
    public sealed class AnswerAudioPlayer : MonoBehaviour
    {
        private NameAudioPlayer player;
        private AudioClip correct;
        private AudioClip incorrect;

        private void Awake()
        {
            var child = new GameObject("AnswerAudio");
            child.transform.SetParent(transform, false);
            player = child.AddComponent<NameAudioPlayer>();
            correct = Resources.Load<AudioClip>("Feedback/sfx_correct");
            incorrect = Resources.Load<AudioClip>("Feedback/sfx_line_wrong");
            if (correct == null || incorrect == null)
                Debug.LogWarning("Resources/Feedback의 정답 또는 오답 음성이 없습니다. 없는 음성은 건너뜁니다.", this);
        }

        public void Play(AnswerResult result, float speed)
        {
            Stop();
            AudioClip clip = result == AnswerResult.Incorrect ? incorrect : correct;
            if (clip != null) StartCoroutine(player.PlayAndWait(clip, speed));
        }

        public void Stop()
        {
            StopAllCoroutines();
            if (player != null) player.Stop();
        }
        private void OnDisable() => Stop();
        private void OnDestroy() { if (player != null) Destroy(player.gameObject); }
    }
}
