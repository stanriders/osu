// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Rhythm : StrainSkill
    {
        public Rhythm(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            return Math.Pow(RhythmEvaluator.EvaluateDifficultyOf(current), 5);
        }

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return RhythmEvaluator.EvaluateDifficultyOf(current);
        }
    }
}
