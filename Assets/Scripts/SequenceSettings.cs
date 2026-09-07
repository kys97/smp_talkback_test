using System;
using UnityEngine;

namespace NamnyeoChilse
{
    [Serializable]
    public struct SequenceTier
    {
        [SerializeField, Min(0)] private int minimumCombo;
        [SerializeField, Min(1)] private int nameCount;
        [Tooltip("현재 음성이 끝난 뒤 다음 이름까지 기다리는 시간(초).")]
        [SerializeField, Min(0)] private float audioInterval;
        [Tooltip("이름 음성 배속. pitch를 사용하므로 음높이도 함께 바뀝니다.")]
        [SerializeField, Range(0.1f, 3f)] private float playbackSpeed;
        public int MinimumCombo => minimumCombo;
        public int NameCount => nameCount;
        public float AudioInterval => float.IsNaN(audioInterval) || float.IsInfinity(audioInterval)
            ? 0f : Math.Max(0f, audioInterval);
        public float PlaybackSpeed => float.IsNaN(playbackSpeed) || float.IsInfinity(playbackSpeed) || playbackSpeed <= 0
            ? 1f : Mathf.Clamp(playbackSpeed, 0.1f, 3f);
        public SequenceTier(int minimumCombo, int nameCount, float audioInterval = 0f, float playbackSpeed = 1f)
        {
            this.minimumCombo = minimumCombo;
            this.nameCount = nameCount;
            this.audioInterval = audioInterval;
            this.playbackSpeed = playbackSpeed;
        }
    }

    [Serializable]
    public sealed class SequenceSettings
    {
        [SerializeField] private SequenceTier[] tiers =
        {
            new SequenceTier(0, 1, 0f, 1f), new SequenceTier(5, 2, 0f, 1.2f),
            new SequenceTier(10, 3, 0f, 1.4f), new SequenceTier(15, 4, 0f, 1.6f)
        };
        public SequenceSettings() { }
        public SequenceSettings(params SequenceTier[] tiers)
        {
            this.tiers = tiers == null ? Array.Empty<SequenceTier>() : (SequenceTier[])tiers.Clone();
        }
        public SequenceSettings Snapshot() => new SequenceSettings(tiers);
        public int GetNameCount(int combo) => GetTier(combo).NameCount;
        public float GetAudioInterval(int combo) => GetTier(combo).AudioInterval;
        public float GetPlaybackSpeed(int combo) => GetTier(combo).PlaybackSpeed;

        private SequenceTier GetTier(int combo)
        {
            int threshold = -1;
            var selected = new SequenceTier(0, 1);
            if (tiers == null) return selected;
            foreach (SequenceTier tier in tiers)
            {
                if (tier.MinimumCombo < 0 || tier.NameCount < 1 || tier.MinimumCombo > combo) continue;
                if (tier.MinimumCombo > threshold)
                {
                    threshold = tier.MinimumCombo;
                    selected = tier;
                }
                else if (tier.MinimumCombo == threshold && tier.NameCount > selected.NameCount) selected = tier;
            }
            return selected;
        }
    }
}
