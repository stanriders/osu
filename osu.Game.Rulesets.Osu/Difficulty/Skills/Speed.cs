﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : OsuStrainSkill
    {
        private double totalMultiplier => 0.98;
        private double burstMultiplier => 2.2;
        private double staminaMultiplier => 0.02;

        private double currentBurstStrain;
        private double currentStaminaStrain;
        private double currentRhythm;

        public Speed(Mod[] mods)
            : base(mods)
        {
        }

        private double strainDecayBurst(double ms) => Math.Pow(0.1, ms / 1000);
        private double strainDecayStamina(double ms) => Math.Pow(0.1, Math.Pow(ms / 1000, 3.5));

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => (currentBurstStrain * currentRhythm) * strainDecayBurst(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentBurstStrain *= strainDecayBurst(((OsuDifficultyHitObject)current).StrainTime);
            currentRhythm = RhythmEvaluator.EvaluateDifficultyOf(current);
            currentBurstStrain += SpeedEvaluator.EvaluateDifficultyOf(current) * burstMultiplier * currentRhythm;

            currentStaminaStrain *= strainDecayStamina(((OsuDifficultyHitObject)current).StrainTime);
            currentStaminaStrain += StaminaEvaluator.EvaluateDifficultyOf(current) * staminaMultiplier;

            double combinedStrain = currentBurstStrain + currentStaminaStrain;

            return combinedStrain * totalMultiplier;
        }

        public double RelevantNoteCount()
        {
            if (ObjectStrains.Count == 0)
                return 0;

            double maxStrain = ObjectStrains.Max();
            if (maxStrain == 0)
                return 0;

            return ObjectStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxStrain * 12.0 - 6.0))));
        }
    }
}
