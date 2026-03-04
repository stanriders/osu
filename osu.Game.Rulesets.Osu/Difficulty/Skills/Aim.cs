// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public Aim(Mod[] mods, bool includeSliders)
            : base(mods)
        {
            IncludeSliders = includeSliders;
        }

        private double currentStrain;

        private double skillMultiplierAim => 62.0;
        private double skillMultiplierSpeed => 5.5;
        private double skillMultiplierTotal => 1.0;
        private double meanExponent => 1.2;

        private readonly List<double> sliderStrains = new List<double>();

        private double strainDecay(double ms) => Math.Pow(0.15, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) =>
            currentStrain * strainDecay(time - current.Previous(0).StartTime) * skillMultiplierTotal;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double decay = strainDecay(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            double aimDifficulty = AimEvaluator.EvaluateDifficultyOf(current, IncludeSliders);
            double speedDifficulty = SpeedAimEvaluator.EvaluateDifficultyOf(current);
            double flowDifficulty = FlowEvaluator.EvaluateDifficultyOf(current);

            if (Mods.Any(m => m is OsuModTouchDevice))
            {
                aimDifficulty = Math.Pow(aimDifficulty, 0.8);
                speedDifficulty = Math.Pow(speedDifficulty, 0.95);
            }

            if (Mods.Any(m => m is OsuModRelax))
            {
                speedDifficulty *= 0.0;
            }

            double difficulty = Math.Min(
                DifficultyCalculationUtils.Norm(meanExponent, aimDifficulty * skillMultiplierAim, speedDifficulty * skillMultiplierSpeed),
                flowDifficulty * 200);

            currentStrain *= decay;
            currentStrain += difficulty * (1 - decay);

            double totalStrain = currentStrain * skillMultiplierTotal;

            if (current.BaseObject is Slider)
                sliderStrains.Add(totalStrain);

            return totalStrain;
        }

        public double GetDifficultSliders()
        {
            if (sliderStrains.Count == 0)
                return 0;

            double maxSliderStrain = sliderStrains.Max();

            if (maxSliderStrain == 0)
                return 0;

            return sliderStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxSliderStrain * 12.0 - 6.0))));
        }

        public double CountTopWeightedSliders(double difficultyValue)
            => OsuStrainUtils.CountTopWeightedSliders(sliderStrains, difficultyValue);
    }

    public class AgilitySum : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public AgilitySum(Mod[] mods, bool includeSliders)
            : base(mods)
        {
            IncludeSliders = includeSliders;
        }

        private double currentStrain;

        private double skillMultiplierAim => 65.2;
        private double skillMultiplierSpeed => 2.5;
        private double skillMultiplierTotal => 1.0;
        private double meanExponent => 1.2;

        private readonly List<double> sliderStrains = new List<double>();

        private double strainDecayAim(double ms) => Math.Pow(0.15, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) =>
            currentStrain * strainDecayAim(time - current.Previous(0).StartTime) * skillMultiplierTotal;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double decayAim = strainDecayAim(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            double aimDifficulty = AimEvaluator.EvaluateDifficultyOf(current, IncludeSliders);
            double speedDifficulty = SpeedAimEvaluator.EvaluateDifficultyOf(current);

            if (Mods.Any(m => m is OsuModTouchDevice))
            {
                aimDifficulty = Math.Pow(aimDifficulty, 0.8);
                speedDifficulty = Math.Pow(speedDifficulty, 0.95);
            }

            if (Mods.Any(m => m is OsuModRelax))
            {
                speedDifficulty *= 0.0;
            }

            var difficulty = DifficultyCalculationUtils.Norm(meanExponent, aimDifficulty * skillMultiplierAim, speedDifficulty * skillMultiplierSpeed);

            currentStrain *= decayAim;
            currentStrain += difficulty * (1 - decayAim);

            double totalStrain = currentStrain * skillMultiplierTotal;

            return totalStrain;
        }
    }

    public class Agility : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public Agility(Mod[] mods, bool includeSliders)
            : base(mods)
        {
            IncludeSliders = includeSliders;
        }

        private double currentStrain;

        private double skillMultiplierAim => 65.2;
        private double skillMultiplierSpeed => 2.5;
        private double skillMultiplierTotal => 1.0;
        private double meanExponent => 1.2;

        private readonly List<double> sliderStrains = new List<double>();

        private double strainDecayAim(double ms) => Math.Pow(0.15, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) =>
            currentStrain * strainDecayAim(time - current.Previous(0).StartTime) * skillMultiplierTotal;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double decayAim = strainDecayAim(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            double aimDifficulty = AimEvaluator.EvaluateDifficultyOf(current, IncludeSliders);
            double speedDifficulty = SpeedAimEvaluator.EvaluateDifficultyOf(current);

            if (Mods.Any(m => m is OsuModTouchDevice))
            {
                aimDifficulty = Math.Pow(aimDifficulty, 0.8);
                speedDifficulty = Math.Pow(speedDifficulty, 0.95);
            }

            if (Mods.Any(m => m is OsuModRelax))
            {
                speedDifficulty *= 0.0;
            }

            var difficulty = speedDifficulty * skillMultiplierSpeed;

            currentStrain *= decayAim;
            currentStrain += difficulty * (1 - decayAim);

            double totalStrain = currentStrain * skillMultiplierTotal;

            return totalStrain;
        }
    }

    public class NoAgility : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public NoAgility(Mod[] mods, bool includeSliders)
            : base(mods)
        {
            IncludeSliders = includeSliders;
        }

        private double currentStrain;

        private double skillMultiplierAim => 65.2;
        private double skillMultiplierSpeed => 2.5;
        private double skillMultiplierTotal => 1.0;
        private double meanExponent => 1.2;

        private readonly List<double> sliderStrains = new List<double>();

        private double strainDecay(double ms) => Math.Pow(0.15, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) =>
            currentStrain * strainDecay(time - current.Previous(0).StartTime) * skillMultiplierTotal;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double decay = strainDecay(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            double aimDifficulty = AimEvaluator.EvaluateDifficultyOf(current, IncludeSliders);
            double speedDifficulty = SpeedAimEvaluator.EvaluateDifficultyOf(current);

            if (Mods.Any(m => m is OsuModTouchDevice))
            {
                aimDifficulty = Math.Pow(aimDifficulty, 0.8);
                speedDifficulty = Math.Pow(speedDifficulty, 0.95);
            }

            if (Mods.Any(m => m is OsuModRelax))
            {
                speedDifficulty *= 0.0;
            }

            double difficulty = aimDifficulty * skillMultiplierAim;

            currentStrain *= decay;
            currentStrain += difficulty * (1 - decay);

            double totalStrain = currentStrain * skillMultiplierTotal;
            return totalStrain;
        }
    }

    public class Flow : OsuStrainSkill
    {
        public readonly bool IncludeSliders;

        public Flow(Mod[] mods, bool includeSliders)
            : base(mods)
        {
            IncludeSliders = includeSliders;
        }

        private double currentStrain;

        private double skillMultiplierAim => 65.2;
        private double skillMultiplierSpeed => 2.5;
        private double skillMultiplierTotal => 1.0;
        private double meanExponent => 1.2;

        private readonly List<double> sliderStrains = new List<double>();

        private double strainDecayAim(double ms) => Math.Pow(0.15, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) =>
            currentStrain * strainDecayAim(time - current.Previous(0).StartTime) * skillMultiplierTotal;

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double decayAim = strainDecayAim(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            double flowDifficulty = FlowEvaluator.EvaluateDifficultyOf(current);

            double difficulty = flowDifficulty * 120;

            currentStrain *= decayAim;
            currentStrain += difficulty * (1 - decayAim);

            double totalStrain = currentStrain * skillMultiplierTotal;

            return totalStrain;
        }
    }
}
