using System;
using UnityEngine;

namespace NamnyeoChilse
{
    [Serializable]
    public struct ComboTier
    {
        [SerializeField, Min(0)] private int minimumCombo;
        [SerializeField, Min(1)] private int multiplier;

        public int MinimumCombo => minimumCombo;
        public int Multiplier => multiplier;

        public ComboTier(int minimumCombo, int multiplier)
        {
            this.minimumCombo = minimumCombo;
            this.multiplier = multiplier;
        }
    }

    [Serializable]
    public sealed class ScoringSettings
    {
        [SerializeField, Min(1)] private int baseScore = 100;
        [Tooltip("시작 콤보 이상에서 적용합니다. 다음 구간 시작 전까지 유지됩니다.")]
        [SerializeField] private ComboTier[] tiers =
        {
            new ComboTier(0, 1), new ComboTier(5, 2),
            new ComboTier(10, 3), new ComboTier(15, 4)
        };

        public int BaseScore => Math.Max(1, baseScore);

        public ScoringSettings() { }

        public ScoringSettings(int baseScore, params ComboTier[] tiers)
        {
            this.baseScore = baseScore;
            this.tiers = tiers == null ? Array.Empty<ComboTier>() : (ComboTier[])tiers.Clone();
        }

        public ScoringSettings Snapshot() => new ScoringSettings(BaseScore, tiers);

        public int GetMultiplier(int combo)
        {
            int threshold = -1;
            int result = 1;
            if (tiers == null) return result;
            foreach (ComboTier tier in tiers)
            {
                if (tier.MinimumCombo < 0 || tier.Multiplier < 1 || tier.MinimumCombo > combo)
                    continue;
                if (tier.MinimumCombo > threshold)
                {
                    threshold = tier.MinimumCombo;
                    result = tier.Multiplier;
                }
                else if (tier.MinimumCombo == threshold)
                    result = Math.Max(result, tier.Multiplier);
            }
            return result;
        }
    }
}
