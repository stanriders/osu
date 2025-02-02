// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Reading : StrainSkill
    {
        private readonly double preempt;
        private readonly double clockRate;
        private double skillMultiplier => 1.0;

        private double currentStrain;

        public Reading(Mod[] mods, double preempt, double clockRate)
            : base(mods)
        {
            this.preempt = preempt;
            this.clockRate = clockRate;
        }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain = ReadingEvaluator.CalculateReadingDiff(current, RhythmEvaluator.EvaluateDifficultyOf(current), clockRate, preempt, Mods.Any(x => x is OsuModHidden)) * skillMultiplier;

            return currentStrain;
        }

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return currentStrain;
        }
    }
}
