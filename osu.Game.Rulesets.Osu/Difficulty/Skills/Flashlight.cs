// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to memorise and hit every object in a map with the Flashlight mod enabled.
    /// </summary>
    public class Flashlight : StrainSkill
    {
        public Flashlight(Mod[] mods)
            : base(mods)
        {
        }

        private double skillMultiplier => 0.056;
        private double strainDecayBase => 0.15;

        private double currentStrain;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            double difficulty = FlashlightEvaluator.EvaluateDifficultyOf(current, Mods);

            difficulty *= 0.98 + Math.Pow(Math.Max(0, CalculateRateAdjustedOverallDifficulty(current)), 2) / 2500;

            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += difficulty * skillMultiplier;

            return currentStrain;
        }

        public static double CalculateRateAdjustedOverallDifficulty(DifficultyHitObject current)
        {
            double hitWindowGreat = current.HitWindow(HitResult.Great) / current.ClockRate;

            return (79.5 - hitWindowGreat) / 6;
        }

        public override double DifficultyValue() => GetCurrentStrainPeaks().Sum();

        public static double DifficultyToPerformance(double difficulty) => 25 * Math.Pow(difficulty, 2);
    }
}
