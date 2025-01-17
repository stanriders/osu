// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

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
            var odho = (OsuDifficultyHitObject)current;
            return Math.Pow(RhythmEvaluator.EvaluateDifficultyOf(current), 20) / odho.StrainTime;
        }

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return RhythmEvaluator.EvaluateDifficultyOf(current);
        }
    }
}
