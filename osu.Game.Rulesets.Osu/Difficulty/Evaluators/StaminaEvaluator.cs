﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class StaminaEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            // derive strainTime for calculation
            var osuCurrObj = (OsuDifficultyHitObject)current;

            double strainTime = osuCurrObj.StrainTime;
            double speedBonus = 0.0;

            if (DifficultyCalculationUtils.MillisecondsToBPM(strainTime) > 220)
                speedBonus = Math.Pow((DifficultyCalculationUtils.BPMToMilliseconds(220) - strainTime) / 40, 2);

            return (1 + speedBonus) * 1000 / strainTime;
        }
    }
}
