// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Reading : StrainSkill
    {
        private readonly double clockRate;
        private double skillMultiplier => 1.0;

        private double currentStrain;
        protected override double DecayWeight => 0.99;

        public Reading(Mod[] mods, double clockRate)
            : base(mods)
        {
            this.clockRate = clockRate;
        }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain = ReadingEvaluator.CalculateReadingDiff(current, RhythmEvaluator.EvaluateDifficultyOf(current), clockRate, Mods.Any(x => x is OsuModHidden)) * skillMultiplier;

            return currentStrain;
        }

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return currentStrain;
        }
    }
}
